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

    EnemyStartPosSetting startPosSetting;

    // stun
    public ParticleSystem stunParticle;
    private bool isStunned = false;

    void Start()
    {
        // Set initial rot safely — player may not exist yet
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            lastPlayerPos = player.position;
        }

        SetInitialRot();
        InitializePool();
        StartPos();
    }

    void SetInitialRot()
    {
        if (player != null)
            transform.rotation = Quaternion.LookRotation(player.position - transform.position);
    }

    void StartPos()
    {
        startPosSetting = GetComponent<EnemyStartPosSetting>();
        if (startPosSetting != null && player != null)
            startPosSetting.CheckAndSetYPos(player);
    }

    void Update()
    {
        if (player == null || isStunned) return;

        TrackPlayerVelocity();
        RotateBodyAndShootPivot();
    }

    // --------------------------------------------------------------------
    // VELOCITY TRACKING
    // --------------------------------------------------------------------
    void TrackPlayerVelocity()
    {
        playerVelocity = (player.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.position;
    }

    // --------------------------------------------------------------------
    // ROTATION
    // --------------------------------------------------------------------
    void RotateBodyAndShootPivot()
    {
        if (!bodyPivot || !shootPivot) return;

        // Horizontal body rotation
        Vector3 toPlayerFromBody = player.position - bodyPivot.position;
        Vector3 flatDir = new Vector3(toPlayerFromBody.x, 0f, toPlayerFromBody.z);

        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion bodyTarget = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            bodyPivot.rotation = Quaternion.Slerp(bodyPivot.rotation, bodyTarget, Time.deltaTime * rotationSpeed);
        }

        // Angle calc
        Vector3 toTargetFromShoot = player.position - shootPivot.position;
        float distance = new Vector3(toTargetFromShoot.x, 0f, toTargetFromShoot.z).magnitude;
        float height = toTargetFromShoot.y;
        float g = Mathf.Abs(Physics.gravity.y);
        float v = projectileSpeed;

        float launchAngleDeg = fallbackLaunchAngle;
        float inside = v * v * v * v - g * (g * distance * distance + 2f * height * v * v);

        if (inside >= 0f && distance > 0.01f)
        {
            float sqrt = Mathf.Sqrt(inside);
            float angle1 = Mathf.Atan((v * v + sqrt) / (g * distance)) * Mathf.Rad2Deg;
            float angle2 = Mathf.Atan((v * v - sqrt) / (g * distance)) * Mathf.Rad2Deg;

            launchAngleDeg = Mathf.Clamp(Mathf.Max(angle1, angle2), minLaunchAngleClamp, maxLaunchAngleClamp);
        }

        Quaternion pitchTarget = Quaternion.Euler(-launchAngleDeg, 0f, 0f);
        shootPivot.localRotation = Quaternion.Slerp(shootPivot.localRotation, pitchTarget, Time.deltaTime * rotationSpeed);
    }

    // --------------------------------------------------------------------
    // POOLING
    // --------------------------------------------------------------------
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
        GameObject proj = projectilePool[poolIndex];
        poolIndex = (poolIndex + 1) % projectilePool.Count;
        return proj;
    }

    // --------------------------------------------------------------------
    // SHOOT LOOP (ONLY STARTED IN OnEnable)
    // --------------------------------------------------------------------
    IEnumerator ShootLoop()
    {
        while (true)
        {
            float delay = Random.Range(minShootDelay, maxShootDelay);
            yield return new WaitForSeconds(delay);

            if (!isStunned)
                FireMortar();
        }
    }

    void FireMortar()
    {
        if (player == null || shootPos == null || isStunned)
            return;

        // Predictive aim
        Vector3 predicted = PredictImpactPoint(player.position, playerVelocity, projectileSpeed);

        // Add horizontal landing error
        Vector2 rnd = Random.insideUnitCircle * Random.Range(0f, horizontalErrorRange);
        predicted += new Vector3(rnd.x, 0f, rnd.y);

        GameObject proj = GetProjectileFromPool();
        proj.transform.position = shootPos.position;
        proj.transform.rotation = shootPos.rotation;
        proj.SetActive(true);

        Rigidbody rb = proj.GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 launchDir = shootPos.forward.normalized;
        Vector3 desiredVelocity = launchDir * projectileSpeed;

        rb.AddForce(desiredVelocity * rb.mass * 0.95f, ForceMode.Impulse);

        proj.GetComponent<Enemy2Projectile>()?.ActivateProjectile();
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
    // STUN EVENTS
    // --------------------------------------------------------------------
    public void OnStunned()
    {
        if (isStunned) return;

        isStunned = true;

        if (shootRoutine != null)
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
        }

        stunParticle?.Play();
    }

    public void OnUnstunned()
    {
        if (!isStunned) return;

        isStunned = false;
        stunParticle?.Stop();

        // DO NOT restart ShootLoop here — it's handled by OnEnable()
    }

    // --------------------------------------------------------------------
    // ENABLE / DISABLE (POOL SAFE)
    // --------------------------------------------------------------------
    void OnEnable()
    {
        isStunned = false;

        stunParticle?.Stop();

        // ensure player reference exists
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        // reset velocity tracking
        if (player != null)
            lastPlayerPos = player.position;

        playerVelocity = Vector3.zero;

        // reset orientation
        SetInitialRot();

        // START SHOOT LOOP — ONLY PLACE IN THE SCRIPT
        shootRoutine = StartCoroutine(ShootLoop());
    }

    void OnDisable()
    {
        if (shootRoutine != null)
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
        }

        stunParticle?.Stop();
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
