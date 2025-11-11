using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy2 : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The main rotating base that turns horizontally toward the player.")]
    public Transform bodyPivot;

    [Tooltip("The vertical pivot that adjusts pitch for aiming the projectile.")]
    public Transform shootPivot;

    [Tooltip("Projectile spawn point (should be a child of shootPivot, ~1m forward).")]
    public Transform shootPos;

    [Tooltip("Projectile prefab to shoot.")]
    public GameObject projectilePrefab;

    [Header("Settings")]
    public float minShootDelay = 5f;
    public float maxShootDelay = 7f;
    public float launchAngle = 45f;           // degrees
    public float projectileSpeed = 20f;
    public float horizontalErrorRange = 1.5f;
    public float rotationSpeed = 3f;
    public int projectilePoolSize = 10;

    private Transform player;
    private Vector3 lastPlayerPos;
    private Vector3 playerVelocity;
    private Coroutine shootRoutine;

    private List<GameObject> projectilePool;
    private int poolIndex = 0;

    void Start()
    {
        // Find player automatically
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            lastPlayerPos = player.position;
        }

        InitializePool();
        shootRoutine = StartCoroutine(ShootLoop());
    }

    void Update()
    {
        if (player == null) return;

        TrackPlayerVelocity();
        RotateBodyAndShootPivot();
    }

    // --- PLAYER TRACKING ---
    void TrackPlayerVelocity()
    {
        playerVelocity = (player.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.position;
    }

    // --- ROTATION ---
    void RotateBodyAndShootPivot()
    {
        if (!bodyPivot || !shootPivot || !player) return;

        Vector3 targetDir = player.position - bodyPivot.position;

        // Horizontal rotation only (Y-axis)
        Vector3 flatDir = new Vector3(targetDir.x, 0f, targetDir.z);
        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion bodyRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            bodyPivot.rotation = Quaternion.Slerp(bodyPivot.rotation, bodyRot, Time.deltaTime * rotationSpeed);
        }

        // Adjust shootPivot pitch to aim towards predicted arc direction (optional refinement)
        Vector3 localTargetDir = bodyPivot.InverseTransformPoint(player.position);
        Vector3 flatLocal = new Vector3(localTargetDir.x, 0f, localTargetDir.z);
        float heightOffset = localTargetDir.y;
        float flatDist = flatLocal.magnitude;

        // simple vertical aiming angle based on launch arc height
        float desiredPitch = Mathf.Atan2(heightOffset + 1.0f, flatDist) * Mathf.Rad2Deg;
        Quaternion pitchRot = Quaternion.Euler(-desiredPitch, 0f, 0f);
        shootPivot.localRotation = Quaternion.Slerp(shootPivot.localRotation, pitchRot, Time.deltaTime * rotationSpeed);
    }

    // --- POOL SETUP ---
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

    // --- SHOOT LOOP ---
    IEnumerator ShootLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minShootDelay, maxShootDelay));
            FireMortar();
        }
    }

    void FireMortar()
    {
        if (!player || !shootPos) return;

        Vector3 targetPos = PredictImpactPoint(player.position, playerVelocity, projectileSpeed);
        Vector2 randomOffset = Random.insideUnitCircle * Random.Range(0f, horizontalErrorRange);
        targetPos += new Vector3(randomOffset.x, 0f, randomOffset.y);

        GameObject proj = GetProjectileFromPool();
        proj.transform.position = shootPos.position;
        proj.transform.rotation = Quaternion.identity;
        proj.SetActive(true);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Vector3 velocity = CalculateLaunchVelocity(shootPos.position, targetPos, launchAngle * Mathf.Deg2Rad);
            rb.linearVelocity = velocity;
        }

        // Reset projectile logic
        Enemy2Projectile projectile = proj.GetComponent<Enemy2Projectile>();
        if (projectile)
            projectile.ActivateProjectile();
    }

    // --- TRAJECTORY CALCULATION ---
    Vector3 PredictImpactPoint(Vector3 playerPos, Vector3 playerVel, float speed)
    {
        Vector3 toPlayer = playerPos - shootPos.position;
        float distance = toPlayer.magnitude;
        float flightTime = distance / speed;
        return playerPos + playerVel * flightTime;
    }

    Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float angleRad)
    {
        Vector3 toTarget = target - start;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float distance = toTargetXZ.magnitude;
        float yOffset = toTarget.y;

        float gravity = Physics.gravity.magnitude;
        float v2 = (gravity * distance * distance) /
                   (2f * (yOffset - Mathf.Tan(angleRad) * distance) * Mathf.Pow(Mathf.Cos(angleRad), 2f));
        v2 = Mathf.Max(0f, v2);
        float v = Mathf.Sqrt(v2);

        Vector3 velocity = toTargetXZ.normalized * v * Mathf.Cos(angleRad);
        velocity.y = v * Mathf.Sin(angleRad);
        return velocity;
    }
}
