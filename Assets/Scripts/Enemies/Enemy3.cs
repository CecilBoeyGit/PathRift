using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class Enemy3 : MonoBehaviour
{
    [Header("References")]
    public Transform laserOrigin;     // The point the laser starts from
    public Transform bodyPivot;       // Optional: rotates horizontally to face player
    public ParticleSystem laserImpact; // Optional impact effect on hit point

    [Header("Laser Settings")]
    public float laserDurationMin = 5f;
    public float laserDurationMax = 6f;
    public float cooldownMin = 4f;
    public float cooldownMax = 6f;
    public float maxLaserDistance = 50f;

    [Tooltip("Damage per second to the player while hit by laser.")]
    public float laserDPS = 15f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 3f;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    // --- stun and dmg state ---
    private bool isStunned = false;

    // internal
    private Transform player;
    private LineRenderer lineRenderer;
    private Coroutine laserRoutine;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (!isStunned)
            laserRoutine = StartCoroutine(LaserLoop());
    }

    void Update()
    {
        if (player == null) return;

        // Rotate to face player horizontally
        if (bodyPivot != null && !isStunned)
        {
            Vector3 dir = player.position - bodyPivot.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                bodyPivot.rotation = Quaternion.Slerp(bodyPivot.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }

        // Keep drawing the beam while active
        if (lineRenderer.enabled && !isStunned)
        {
            FireLaser();
        }
    }

    // -----------------------------------------------------
    // Laser loop logic (fire -> cooldown -> repeat)
    // -----------------------------------------------------
    IEnumerator LaserLoop()
    {
        while (true)
        {
            // Wait before firing
            yield return new WaitForSeconds(Random.Range(cooldownMin, cooldownMax));
            if (isStunned) continue;

            // Fire laser
            float laserDuration = Random.Range(laserDurationMin, laserDurationMax);
            lineRenderer.enabled = true;
            float timer = 0f;

            while (timer < laserDuration && !isStunned)
            {
                FireLaser();
                timer += Time.deltaTime;
                yield return null;
            }

            // Stop firing
            lineRenderer.enabled = false;
            if (laserImpact != null && laserImpact.isPlaying)
                laserImpact.Stop();
        }
    }

    // -----------------------------------------------------
    // Laser Raycast + Damage
    // -----------------------------------------------------
    void FireLaser()
    {
        if (laserOrigin == null || player == null) return;

        Vector3 origin = laserOrigin.position;
        Vector3 direction = (player.position - origin).normalized;

        // Perform raycast
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, maxLaserDistance))
        {
            // Draw beam
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, hit.point);

            // Impact VFX
            if (laserImpact != null)
            {
                laserImpact.transform.position = hit.point;
                if (!laserImpact.isPlaying) laserImpact.Play();
            }

            // Deal damage if player hit
            if (hit.collider.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // Continuous beam damage: compound across multiple lasers
                    playerHealth.DealLaserDamage(laserDPS * Time.deltaTime);
                }
            }
        }
        else
        {
            // No hit - draw to max distance
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin + direction * maxLaserDistance);

            if (laserImpact != null && laserImpact.isPlaying)
                laserImpact.Stop();
        }
    }

    // -----------------------------------------------------
    // UnityEvent Hook Functions for Stun / Damage
    // -----------------------------------------------------
    public void OnStunned()
    {
        if (isStunned) return;
        isStunned = true;

        // Stop laser visuals and damage
        if (laserRoutine != null)
            StopCoroutine(laserRoutine);
        lineRenderer.enabled = false;
        if (laserImpact != null) laserImpact.Stop();

        Debug.Log($"{name} stunned — laser disabled");
    }

    public void OnUnstunned()
    {
        if (!isStunned) return;
        isStunned = false;

        // Resume attack loop
        if (laserRoutine == null)
            laserRoutine = StartCoroutine(LaserLoop());

        Debug.Log($"{name} recovered from stun — laser active again");
    }

    public void DealDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{name} destroyed");
        StopAllCoroutines();
        lineRenderer.enabled = false;
        if (laserImpact != null) laserImpact.Stop();
        gameObject.SetActive(false);
    }
}
