using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy2 : MonoBehaviour
{
    [Header("References")]
    public Transform bodyPivot;
    public Transform shootPivot;
    public Transform shootPos;
    public GameObject projectilePrefab;

    [Header("Settings")]
    public float minShootDelay = 5f;
    public float maxShootDelay = 7f;
    public float projectileSpeed = 20f;
    public float horizontalErrorRange = 1.5f;
    public float rotationSpeed = 3f;
    public int projectilePoolSize = 10;
    public float fallbackLaunchAngle = 60f;

    [Header("Visual Angle Clamp (degrees)")]
    public float minLaunchAngleClamp = 55f;
    public float maxLaunchAngleClamp = 70f;

    private Transform player;
    private Vector3 lastPlayerPos;
    private Vector3 playerVelocity;
    private Coroutine shootRoutine;

    private List<GameObject> projectilePool;
    private int poolIndex = 0;

    // --- stun handling ---
    public ParticleSystem stunParticle;
    private bool isStunned = false;

    void Start()
    {
        SetInitialRot();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            lastPlayerPos = player.position;
        }

        InitializePool();

        if (!isStunned)
            shootRoutine = StartCoroutine(ShootLoop());
    }

    void SetInitialRot()
    {
        transform.rotation = Quaternion.LookRotation(player.position - transform.position);
    }

    void Update()
    {
        if (player == null || isStunned) return;

        TrackPlayerVelocity();
        RotateBodyAndShootPivot();
    }

    // -------------------------
    // Player velocity tracking
    // -------------------------
    void TrackPlayerVelocity()
    {
        playerVelocity = (player.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.position;
    }

    // --------------------------------------------------------
    // Rotate body (Y only) and pitch shootPivot for mortar arc
    // --------------------------------------------------------
    void RotateBodyAndShootPivot()
    {
        if (!bodyPivot || !shootPivot) return;

        Vector3 toPlayerFromBody = player.position - bodyPivot.position;
        Vector3 flatDir = new Vector3(toPlayerFromBody.x, 0f, toPlayerFromBody.z);
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion bodyTarget = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            bodyPivot.rotation = Quaternion.Slerp(bodyPivot.rotation, bodyTarget, Time.deltaTime * rotationSpeed);
        }

        Vector3 toTargetFromShoot = player.position - shootPivot.position;
        float distance = new Vector3(toTargetFromShoot.x, 0f, toTargetFromShoot.z).magnitude;
        float height = toTargetFromShoot.y;
        float g = Mathf.Abs(Physics.gravity.y);
        float v = projectileSpeed;

        float launchAngleDeg = fallbackLaunchAngle;
        float inside = v * v * v * v - g * (g * distance * distance + 2f * height * v * v);
        if (inside >= 0f && distance > 0.001f)
        {
            float sqrt = Mathf.Sqrt(inside);
            float angle1 = Mathf.Atan((v * v + sqrt) / (g * distance)) * Mathf.Rad2Deg;
            float angle2 = Mathf.Atan((v * v - sqrt) / (g * distance)) * Mathf.Rad2Deg;

            launchAngleDeg = Mathf.Clamp(Mathf.Max(angle1, angle2), minLaunchAngleClamp, maxLaunchAngleClamp);
        }

        Quaternion pitchTarget = Quaternion.Euler(-launchAngleDeg, 0f, 0f);
        shootPivot.localRotation = Quaternion.Slerp(shootPivot.localRotation, pitchTarget, Time.deltaTime * rotationSpeed);
    }

    // -------------------------
    // Pooling
    // -------------------------
    void InitializePool()
    {
        projectilePool = new List<GameObject>(projectilePoolSize);
        for (int i = 0; i < projectilePoolSize; i++)
        {
            GameObject proj = Instantiate(projectilePrefab);
            proj.SetActive(false);
            projectilePool.Add(proj);
        }
    }

    GameObject GetProjectileFromPool()
    {
        if (projectilePool == null || projectilePool.Count == 0)
            return null;

        GameObject proj = projectilePool[poolIndex];
        poolIndex = (poolIndex + 1) % projectilePool.Count;
        return proj;
    }

    // -------------------------
    // Shooting loop
    // -------------------------
    IEnumerator ShootLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minShootDelay, maxShootDelay));
            if (!isStunned)
                FireMortar();
        }
    }

    void FireMortar()
    {
        if (isStunned || player == null || shootPos == null) return;

        Vector3 predicted = PredictImpactPoint(player.position, playerVelocity, projectileSpeed);
        Vector2 rnd = Random.insideUnitCircle * Random.Range(0f, horizontalErrorRange);
        predicted += new Vector3(rnd.x, 0f, rnd.y);

        GameObject proj = GetProjectileFromPool();
        if (proj == null) return;

        proj.transform.position = shootPos.position;
        proj.transform.rotation = shootPos.rotation;
        proj.SetActive(true);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Vector3 launchDir = shootPos.forward.normalized;
            Vector3 desiredVelocity = launchDir * projectileSpeed;
            rb.AddForce(desiredVelocity * rb.mass * 0.95f, ForceMode.Impulse);
        }

        Enemy2Projectile pScript = proj.GetComponent<Enemy2Projectile>();
        if (pScript != null)
            pScript.ActivateProjectile();
    }

    Vector3 PredictImpactPoint(Vector3 playerPos, Vector3 playerVel, float speed)
    {
        Vector3 toPlayer = playerPos - (shootPos != null ? shootPos.position : transform.position);
        float distance = toPlayer.magnitude;
        if (speed <= 0f) return playerPos;
        float flightTime = distance / speed;
        return playerPos + playerVel * flightTime;
    }

    // --------------------------------------------------------------------
    // UNITY EVENT HOOKS (plug into EnemyHealth events)
    // --------------------------------------------------------------------
    public void OnStunned()
    {
        if (isStunned) return;
        isStunned = true;
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);
        
        stunParticle.Play();
        shootRoutine = null;
        Debug.Log($"{name} stunned — mortar disabled");
    }

    public void OnUnstunned()
    {
        if (!isStunned) return;
        isStunned = false;
        stunParticle.Stop();
        if (shootRoutine == null)
            shootRoutine = StartCoroutine(ShootLoop());
        Debug.Log($"{name} recovered from stun — mortar active again");
    }

    void OnEnable()
    {
        // Reset stun state
        isStunned = false;

        // Stop particles just in case
        if (stunParticle != null)
            stunParticle.Stop();

        // Reset player velocity tracking
        playerVelocity = Vector3.zero;

        // Reset last player position
        if (player != null)
            lastPlayerPos = player.position;

        // Stop orphaned coroutine
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);
        shootRoutine = null;

        // Reset rotation
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }
        SetInitialRot();

        // Start firing loop again
        if (!isStunned)
            shootRoutine = StartCoroutine(ShootLoop());
    }

    void OnDisable()
    {
        // Stop shoot routine
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);
        shootRoutine = null;

        // Stop stun particle if still active
        if (stunParticle != null)
            stunParticle.Stop();
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (shootPos != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(shootPos.position, shootPos.position + shootPos.forward * 6f);
        }
    }
#endif
}
