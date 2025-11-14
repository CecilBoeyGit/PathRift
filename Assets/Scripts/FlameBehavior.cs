using System.Collections;
using UnityEngine;

public class FlameBehavior : MonoBehaviour
{
    private Vector3 targetScale;
    private Coroutine activeRoutine;
    public ParticleSystem particle;
    private float dmgMultiplier = 1f;

    void OnEnable()
    {
        // Stop previous coroutine if pooled
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        if (particle)
            particle.Play();

        // Start small for growth animation
        transform.localScale = Vector3.zero;

        activeRoutine = StartCoroutine(GrowAndAutoDisable());
    }

    // ---- Public API ----
    public void SetTargetScale(Vector3 newScale)
    {
        targetScale = newScale;
    }

    public void SetDamageMult(float dmgMult)
    {
        dmgMultiplier = dmgMult;
    }

    // ---- Growth + Lifetime + Shrink ----
    private IEnumerator GrowAndAutoDisable()
    {
        yield return null;
        // Grow from 0 → targetScale
        yield return StartCoroutine(GrowToFullSize());

        // Stay active for a random amount of time
        float waitTime = Random.Range(8f, 10f);
        yield return new WaitForSeconds(waitTime);

        // Shrink from targetScale → 0
        yield return StartCoroutine(ShrinkToZero());

        // Reset for pooling
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive(false);
        activeRoutine = null;
    }

    private IEnumerator GrowToFullSize()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private IEnumerator ShrinkToZero()
    {
        particle.Stop();
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.zero;
    }

    // ---- Damage application ----
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                // Example: apply only if greater than previously applied dmgMultiplier
                if (enemyHealth.dmgDuration <= dmgMultiplier)
                    enemyHealth.dmgDuration = dmgMultiplier;
            }
        }
    }
}
