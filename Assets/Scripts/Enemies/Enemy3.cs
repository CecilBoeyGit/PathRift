using UnityEngine;
using System.Collections;
using System.Threading;

[RequireComponent(typeof(LineRenderer))]
public class Enemy3 : MonoBehaviour
{
    [Header("References")]
    public Transform laserOrigin;     // Where the laser starts
    public Transform bodyPivot;       // Rotates horizontally toward player
    public ParticleSystem laserImpact; // Optional impact VFX

    [Header("Laser Settings")]
    public float laserDurationMin = 5f;
    public float laserDurationMax = 6f;
    public float cooldownMin = 4f;
    public float cooldownMax = 6f;
    public float maxLaserDistance = 50f;
    public float laserYoffsetMax = 0.08f;
    float currentLaserYOffset;

    [Tooltip("Damage per second the laser applies to the player.")]
    public float laserDPS = 15f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 3f;

    // internal
    private Transform player;
    private LineRenderer lineRenderer;
    private Coroutine laserRoutine;
    private bool isStunned = false;

    public bool unfollow = false;
    public float trackTimer = 1;

    void Start()
    {
        SetInitialRot();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (!isStunned)
            laserRoutine = StartCoroutine(LaserLoop());
    }

    void SetInitialRot()
    {
        transform.rotation = Quaternion.LookRotation(player.position - transform.position);
    }

    void Update()
    {
        if (!unfollow)
        {
            trackTimer -= Time.deltaTime;
        }
        else
        {
            trackTimer = 1;
        }

        if (player == null) return;

        if (!isStunned)
            RotateTowardsPlayer();

        if (lineRenderer.enabled && !isStunned)
            FireLaser(); // continuously update laser while firing
    }

    // -----------------------------------------------------
    // Rotation logic
    // -----------------------------------------------------
    void RotateTowardsPlayer()
    {
        if (bodyPivot == null) return;

        Vector3 dir = (player.position - new Vector3(0, 0.35f, 0)) - bodyPivot.position;
        dir.y = 0f; // horizontal-only rotation

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            bodyPivot.rotation = Quaternion.Slerp(bodyPivot.rotation, target, Time.deltaTime * rotationSpeed);
        }
    }

    // -----------------------------------------------------
    // Laser Loop (fire → cooldown → repeat)
    // -----------------------------------------------------
    IEnumerator LaserLoop()
    {
        while (true)
        {
            // cooldown before firing
            yield return new WaitForSeconds(Random.Range(cooldownMin, cooldownMax));
            if (isStunned) continue;

            // Start firing
            lineRenderer.enabled = true;
            float fireTime = Random.Range(laserDurationMin, laserDurationMax);
            currentLaserYOffset = Random.Range(-laserYoffsetMax, laserYoffsetMax);

            float timer = 0f;
            while (timer < fireTime && !isStunned)
            {
                FireLaser();
                timer += Time.deltaTime;
                yield return null;
            }

            // stop laser
            lineRenderer.enabled = false;
            if (laserImpact != null) laserImpact.Stop();
        }
    }

    // -----------------------------------------------------
    // Laser Raycast + Damage
    // -----------------------------------------------------
    void FireLaser()
    {
        if (laserOrigin == null || player == null)
            return;

        Vector3 origin = laserOrigin.position;
        Vector3 direction = ((player.position - new Vector3(0, 0.35f, 0)) - origin).normalized;

        // Raycast
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxLaserDistance))
        {
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, hit.point);

            // Impact effect
            if (laserImpact != null)
            {
                laserImpact.transform.position = hit.point;
                if (!laserImpact.isPlaying) laserImpact.Play();
            }

            // Deal damage if we hit player
            if (hit.collider.CompareTag("Player"))
            {
                PlayerHealth health = hit.collider.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    // Continuous beam damage — *compounds* from multiple Enemy3
                    health.DealLaserDamage(laserDPS * Time.deltaTime);
                }
            }
        }
        else
        {
            // Laser hit nothing
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin + direction * maxLaserDistance);

            if (laserImpact != null && laserImpact.isPlaying)
                laserImpact.Stop();
        }
    }

    // -----------------------------------------------------
    // STUN EVENT HOOKS (these are assigned via EnemyHealth)
    // -----------------------------------------------------
    public void OnStunned()
    {
        if (isStunned) return;

        isStunned = true;

        if (laserRoutine != null)
            StopCoroutine(laserRoutine);

        // stop laser visuals and damage
        lineRenderer.enabled = false;
        if (laserImpact != null) laserImpact.Stop();

        Debug.Log($"{name}: Stunned — laser disabled");
    }

    public void OnUnstunned()
    {
        if (!isStunned) return;

        isStunned = false;

        // resume laser loop
        if (laserRoutine == null)
            laserRoutine = StartCoroutine(LaserLoop());

        Debug.Log($"{name}: Recovered from stun — laser re-enabled");
    }

    void OnEnable()
    {
        // Reset stun state
        isStunned = false;

        // Reset laser visuals
        if (lineRenderer != null)
            lineRenderer.enabled = false;

        if (laserImpact != null)
            laserImpact.Stop();

        // Reset timers & tracking
        trackTimer = 1f;
        unfollow = false;
        currentLaserYOffset = 0f;

        // Stop any orphan coroutine
        if (laserRoutine != null)
            StopCoroutine(laserRoutine);
        laserRoutine = null;

        // Reset rotation
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        SetInitialRot();

        // Restart attack loop
        if (!isStunned)
            laserRoutine = StartCoroutine(LaserLoop());
    }

    void OnDisable()
    {
        // Disable laser immediately when disabled
        if (lineRenderer != null)
            lineRenderer.enabled = false;

        if (laserImpact != null)
            laserImpact.Stop();

        // Clean coroutine
        if (laserRoutine != null)
            StopCoroutine(laserRoutine);
        laserRoutine = null;
    }
}
