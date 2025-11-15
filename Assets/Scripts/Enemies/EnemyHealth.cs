using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class EnemyHealth : MonoBehaviour
{
    // ------------------------------------------------------------
    // HEALTH
    // ------------------------------------------------------------
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    // ------------------------------------------------------------
    // STUN (Crystals)
    // ------------------------------------------------------------
    [Header("Stun Settings")]
    public bool isStunned = false;
    public float stunDuration = 3f;
    private float stunTimer = 0f;

    [Header("Stun Events")]
    public UnityEvent onStunned;
    public UnityEvent onUnstunned;

    private readonly HashSet<GameObject> activeCrystals = new HashSet<GameObject>();
    private bool previousStunState = false;

    // ------------------------------------------------------------
    // DAMAGE-OVER-TIME (Flame)
    // ------------------------------------------------------------
    [Header("Flame Damage Settings")]
    public bool isDmged = false;      // whether flame DoT is active
    public float dmgDuration = 3f;    // how long after last flame stays active
    private float dmgTimer = 0f;

    public float dmgRate = 1f;        // 1 HP per second

    private readonly HashSet<GameObject> activeFlames = new HashSet<GameObject>();
    private bool wasDmgedPrevFrame = false;

    // ------------------------------------------------------------
    // Unity Methods
    // ------------------------------------------------------------
    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        UpdateStunState();
        HandleStunEvents();

        UpdateFlameDamage();
        ApplyFlameDamageOverTime();
    }

    // ------------------------------------------------------------
    // DAMAGE
    // ------------------------------------------------------------
    public void DealDamage(float damage)
    {
        if (damage <= 0f) return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------
    // COLLISION HANDLING (Crystals & Flames)
    // ------------------------------------------------------------
    void OnCollisionEnter(Collision col)
    {
        // --- Stun: Crystals ---
        if (col.collider.CompareTag("Crystal"))
        {
            GameObject crystal = col.collider.gameObject;
            if (!activeCrystals.Contains(crystal))
            {
                activeCrystals.Add(crystal);
                isStunned = true;
                stunTimer = stunDuration;
            }
        }

        // --- Damage: Flames ---
        if (col.collider.CompareTag("Flame"))
        {
            GameObject flame = col.collider.gameObject;
            if (!activeFlames.Contains(flame))
            {
                activeFlames.Add(flame);
                isDmged = true;
                dmgTimer = dmgDuration; // refresh DoT timer
            }
        }
    }

    void OnCollisionExit(Collision col)
    {
        // --- Stun: Crystals ---
        if (col.collider.CompareTag("Crystal"))
        {
            GameObject crystal = col.collider.gameObject;
            activeCrystals.Remove(crystal);

            if (activeCrystals.Count == 0)
                stunTimer = stunDuration;
        }

        // --- Flame Damage ---
        if (col.collider.CompareTag("Flame"))
        {
            GameObject flame = col.collider.gameObject;
            activeFlames.Remove(flame);

            if (activeFlames.Count == 0)
                dmgTimer = dmgDuration;
        }
    }

    // ------------------------------------------------------------
    // STUN UPDATE
    // ------------------------------------------------------------
    void UpdateStunState()
    {
        // remove disabled or pooled crystals
        activeCrystals.RemoveWhere(c => c == null || !c.activeInHierarchy);

        if (activeCrystals.Count > 0)
        {
            isStunned = true;
            stunTimer = stunDuration;
        }
        else
        {
            if (isStunned)
            {
                if (stunTimer > 0f)
                    stunTimer -= Time.deltaTime;
                else
                    isStunned = false;
            }
        }
    }

    void HandleStunEvents()
    {
        if (isStunned != previousStunState)
        {
            if (isStunned)
                onStunned.Invoke();
            else
                onUnstunned.Invoke();

            previousStunState = isStunned;
        }
    }

    // ------------------------------------------------------------
    // FLAME DAMAGE UPDATE
    // ------------------------------------------------------------
    void UpdateFlameDamage()
    {
        // cleanup inactive flames
        activeFlames.RemoveWhere(f => f == null || !f.activeInHierarchy);

        if (activeFlames.Count > 0)
        {
            isDmged = true;
            dmgTimer = dmgDuration;
        }
        else
        {
            if (isDmged)
            {
                if (dmgTimer > 0f)
                    dmgTimer -= Time.deltaTime;
                else
                    isDmged = false;
            }
        }
    }

    // ------------------------------------------------------------
    // FLAME DAMAGE-OVER-TIME
    // ------------------------------------------------------------
    void ApplyFlameDamageOverTime()
    {
        if (isDmged)
        {
            // 1 HP per second default
            DealDamage(dmgRate * Time.deltaTime);
        }

        wasDmgedPrevFrame = isDmged;
    }

    void OnEnable()
    {
        // -----------------------
        // RESET HEALTH
        // -----------------------
        currentHealth = maxHealth;

        // -----------------------
        // RESET STUN STATE
        // -----------------------
        isStunned = false;
        stunTimer = 0f;
        activeCrystals.Clear();
        previousStunState = false; // ensures next stun triggers the event

        // -----------------------
        // RESET FLAME DAMAGE (DoT)
        // -----------------------
        isDmged = false;
        dmgTimer = 0f;
        activeFlames.Clear();
        wasDmgedPrevFrame = false;

        // -----------------------
        // OPTIONAL: clear events from previous pooling cycle
        // (UnityEvent listeners remain valid and should not be cleared)
        // -----------------------

        // Enemy will re-awaken in a "clean" state
    }

    void OnDisable()
    {
        // Cleanup timers & states
        stunTimer = 0f;
        dmgTimer = 0f;

        // Clear active collision sets (important for pooling!)
        activeCrystals.Clear();
        activeFlames.Clear();

        // Reset event state tracking
        previousStunState = false;
        wasDmgedPrevFrame = false;
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isStunned ? Color.cyan : Color.gray;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.5f);

        Gizmos.color = isDmged ? Color.red : new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f, 0.35f);
    }
#endif
}
