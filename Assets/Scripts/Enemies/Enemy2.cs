using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy2 : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Main rotating base that turns horizontally toward the player.")]
    public Transform bodyPivot;

    [Tooltip("Pivot that pitches up/down for mortar firing.")]
    public Transform shootPivot;

    [Tooltip("Projectile spawn point (~1m forward, child of shootPivot).")]
    public Transform shootPos;

    [Tooltip("Projectile prefab used for firing (needs Rigidbody).")]
    public GameObject projectilePrefab;

    [Header("Settings")]
    public float minShootDelay = 5f;
    public float maxShootDelay = 7f;
    public float projectileSpeed = 20f;       // m/s (used to compute angles & impulse)
    public float horizontalErrorRange = 1.5f; // random horizontal landing error (meters)
    public float rotationSpeed = 3f;          // smoothing for body & pivot rotation
    public int projectilePoolSize = 10;

    [Tooltip("Base fallback launch angle in degrees if calculation fails.")]
    public float fallbackLaunchAngle = 60f;

    // clamp mortar angle visually to avoid near-vertical shots
    [Header("Visual Angle Clamp (degrees)")]
    public float minLaunchAngleClamp = 55f;
    public float maxLaunchAngleClamp = 70f;

    // internals
    private Transform player;
    private Vector3 lastPlayerPos;
    private Vector3 playerVelocity;
    private Coroutine shootRoutine;

    private List<GameObject> projectilePool;
    private int poolIndex = 0;

    void Start()
    {
        // auto-find player by tag if not assigned
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                lastPlayerPos = player.position;
            }
        }

        if (shootPos == null)
            Debug.LogWarning("Enemy2: shootPos is not assigned.");

        InitializePool();
        shootRoutine = StartCoroutine(ShootLoop());
    }

    void Update()
    {
        if (player == null) return;

        TrackPlayerVelocity();
        RotateBodyAndShootPivot();
    }

    // -------------------------
    // Player velocity tracking
    // -------------------------
    void TrackPlayerVelocity()
    {
        // Works for kinematic or dynamic players
        playerVelocity = (player.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.position;
    }

    // --------------------------------------------------------
    // Rotate body (Y only) and pitch shootPivot for mortar arc
    // --------------------------------------------------------
    void RotateBodyAndShootPivot()
    {
        if (!bodyPivot || !shootPivot || player == null) return;

        // Horizontal rotation (Y axis only)
        Vector3 toPlayerFromBody = player.position - bodyPivot.position;
        Vector3 flatDir = new Vector3(toPlayerFromBody.x, 0f, toPlayerFromBody.z);
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion bodyTarget = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            bodyPivot.rotation = Quaternion.Slerp(bodyPivot.rotation, bodyTarget, Time.deltaTime * rotationSpeed);
        }

        // Mortar-style elevation calculation:
        // Solve for angle given projectile speed, horizontal distance and height difference.
        Vector3 toTargetFromShoot = player.position - shootPivot.position;
        float distance = new Vector3(toTargetFromShoot.x, 0f, toTargetFromShoot.z).magnitude;
        float height = toTargetFromShoot.y;
        float g = Mathf.Abs(Physics.gravity.y);
        float v = projectileSpeed;

        float launchAngleDeg = fallbackLaunchAngle;

        // Use ballistic equation discriminant to test feasibility
        float inside = v * v * v * v - g * (g * distance * distance + 2f * height * v * v);
        if (inside >= 0f && distance > 0.001f)
        {
            float sqrt = Mathf.Sqrt(inside);
            float angle1 = Mathf.Atan((v * v + sqrt) / (g * distance)) * Mathf.Rad2Deg;
            float angle2 = Mathf.Atan((v * v - sqrt) / (g * distance)) * Mathf.Rad2Deg;

            // choose the higher arc for mortar behavior but clamp it to visual limits
            launchAngleDeg = Mathf.Max(angle1, angle2);
            launchAngleDeg = Mathf.Clamp(launchAngleDeg, minLaunchAngleClamp, maxLaunchAngleClamp);
        }
        else
        {
            // fallback clamped angle if calculation fails
            launchAngleDeg = Mathf.Clamp(fallbackLaunchAngle, minLaunchAngleClamp, maxLaunchAngleClamp);
        }

        // Apply pitch to shootPivot (local X axis). Negative because Unity's Look direction uses -X for pitch up in many setups
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
            // ensure rigidbody exists
            if (proj.GetComponent<Rigidbody>() == null)
                Debug.LogWarning("Enemy2: projectilePrefab missing Rigidbody.");
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
            FireMortar();
        }
    }

    // -------------------------
    // Fire logic (uses AddForce impulse)
    // -------------------------
    void FireMortar()
    {
        if (player == null || shootPos == null) return;

        // Predict where player will be
        Vector3 predicted = PredictImpactPoint(player.position, playerVelocity, projectileSpeed);

        // Add horizontal error
        Vector2 rnd = Random.insideUnitCircle * Random.Range(0f, horizontalErrorRange);
        predicted += new Vector3(rnd.x, 0f, rnd.y);

        GameObject proj = GetProjectileFromPool();
        if (proj == null) return;

        proj.transform.position = shootPos.position;
        proj.transform.rotation = shootPos.rotation; // shootPos should reflect pitch & yaw
        proj.SetActive(true);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;

            // reset velocities (Unity 6 uses linearVelocity)
#if UNITY_6_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.linearVelocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;

            // Launch using shootPos.forward (which includes pitch)
            Vector3 launchDir = shootPos.forward.normalized;

            // Compute impulse needed to approximate the magnitude (mass = rb.mass)
            Vector3 desiredVelocity = launchDir * projectileSpeed;
            Vector3 impulse = desiredVelocity * rb.mass; // impulse = mass * velocity

            rb.AddForce(impulse, ForceMode.Impulse);
        }

        // Reactivate projectile logic (explosion + lifetime)
        Enemy2Projectile pScript = proj.GetComponent<Enemy2Projectile>();
        if (pScript != null)
            pScript.ActivateProjectile();
    }

    // Predict naive impact point by dividing straight-line distance by speed
    Vector3 PredictImpactPoint(Vector3 playerPos, Vector3 playerVel, float speed)
    {
        Vector3 toPlayer = playerPos - (shootPos != null ? shootPos.position : transform.position);
        float distance = toPlayer.magnitude;
        if (speed <= 0f) return playerPos;
        float flightTime = distance / speed;
        return playerPos + playerVel * flightTime;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (shootPos != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(shootPos.position, shootPos.position + shootPos.forward * 6f);
        }

        if (player != null && shootPos != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 pred = PredictImpactPoint(player.position, playerVelocity, projectileSpeed);
            Gizmos.DrawSphere(pred, 0.15f);
            Gizmos.DrawLine(shootPos.position, pred);
        }
    }
#endif
}
