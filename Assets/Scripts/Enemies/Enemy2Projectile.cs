using UnityEngine;
using System.Collections;

public class Enemy2Projectile : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 2f;
    public float baseDamage = 30f;
    public ParticleSystem explosionEffect;

    private Rigidbody rb;
    private bool hasExploded = false;
    private Coroutine lifeTimerRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void ActivateProjectile()
    {
        hasExploded = false;

        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Start fail-safe timer
        if (lifeTimerRoutine != null)
            StopCoroutine(lifeTimerRoutine);
        lifeTimerRoutine = StartCoroutine(FailSafeTimer());
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        hasExploded = true;
        StartCoroutine(ExplodeAfterFrame());
    }

    IEnumerator ExplodeAfterFrame()
    {
        yield return null;
        Explode();
    }

    void Explode()
    {
        // Stop lifetime timer
        if (lifeTimerRoutine != null)
            StopCoroutine(lifeTimerRoutine);

        // Play explosion effect
        if (explosionEffect)
        {
            explosionEffect.transform.position = transform.position;
            explosionEffect.Play();
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Vector3 direction = (hit.transform.position - transform.position).normalized;
                RaycastHit rayHit;

                if (Physics.Raycast(transform.position, direction, out rayHit, explosionRadius))
                {
                    if (rayHit.collider.CompareTag("Player"))
                    {
                        float dist = Vector3.Distance(transform.position, hit.transform.position);
                        float damage = CalculateFalloffDamage(dist);
                        PlayerHealth playerHealth = hit.gameObject.GetComponent<PlayerHealth>();
                        playerHealth.DealDamage(12f);
                        Debug.Log($"Player hit by explosion! Damage: {damage:F1}");
                        // TODO: Apply damage to player here
                    }
                }
            }
        }

        StartCoroutine(DisableAfterDelay(0.3f));
    }

    float CalculateFalloffDamage(float distance)
    {
        float t = Mathf.Clamp01(distance / explosionRadius);
        return Mathf.Lerp(baseDamage, 0f, t);
    }

    IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetProjectile();
    }

    IEnumerator FailSafeTimer()
    {
        yield return new WaitForSeconds(10f);
        if (!hasExploded)
        {
            Debug.Log("Projectile lifetime exceeded, disabling.");
            ResetProjectile();
        }
    }

    void ResetProjectile()
    {
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        hasExploded = false;
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
