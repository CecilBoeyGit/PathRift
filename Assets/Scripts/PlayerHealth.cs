using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Player References")]
    public Transform playerHead;

    [Header("UI References")]
    public Transform uiPivot;
    public Canvas healthCanvas;
    public TextMeshProUGUI healthText;

    [Header("UI Settings")]
    public float uiDistance = 1.0f;
    public float pivotYOffset = -0.3f;
    public float orbitSpeed = 0f;

    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent onPlayerDied;

    // -------------------------------
    // Healing from Flames
    // -------------------------------
    private readonly HashSet<GameObject> activeFlames = new HashSet<GameObject>();
    public float healRate = 2f;   // HP per second heal from flames
    private bool isHealing = false;

    void Awake()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        UpdateUIPivotPosition();
        RotateUIPivot();
        UpdateHealing();
    }

    // ------------------------------------------------------------
    // Healing system
    // ------------------------------------------------------------
    private void UpdateHealing()
    {
        // First, remove flames that were pooled or disabled
        activeFlames.RemoveWhere(f => f == null || !f.activeInHierarchy);

        // Check if any flames still touching
        isHealing = activeFlames.Count > 0;

        // Apply healing while in flame contact
        if (isHealing)
        {
            Heal(healRate * Time.deltaTime);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Flame"))
        {
            GameObject flame = col.collider.gameObject;

            // Add only once
            if (!activeFlames.Contains(flame))
                activeFlames.Add(flame);

            isHealing = true;
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (col.collider.CompareTag("Flame"))
        {
            GameObject flame = col.collider.gameObject;

            // Remove safely
            activeFlames.Remove(flame);

            // If no flames remain → stop healing
            if (activeFlames.Count == 0)
                isHealing = false;
        }
    }

    // ------------------------------------------------------------
    // Damage / Heal / UI (unchanged)
    // ------------------------------------------------------------
    public void DealDamage(float damage)
    {
        if (damage <= 0f) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0f)
            Die();
    }

    public void DealLaserDamage(float dmgAmount)
    {
        currentHealth -= dmgAmount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
    }

    void Die()
    {
        Debug.Log("Player has died.");
        onPlayerDied.Invoke();
    }

    // ------------------------------------------------------------
    // UI logic (unchanged)
    // ------------------------------------------------------------
    void UpdateUIPivotPosition()
    {
        if (uiPivot == null || playerHead == null) return;

        Vector3 headPos = playerHead.position;
        uiPivot.position = new Vector3(headPos.x, headPos.y + pivotYOffset, headPos.z);

        if (healthCanvas != null)
        {
            Transform canvasTransform = healthCanvas.transform;
            Vector3 forwardDir = playerHead.forward;
            Vector3 targetPos = uiPivot.position + forwardDir * uiDistance;

            canvasTransform.position = targetPos;
            canvasTransform.rotation = Quaternion.LookRotation(forwardDir, Vector3.up);
        }
    }

    void RotateUIPivot()
    {
        if (uiPivot == null) return;

        if (orbitSpeed != 0f)
            uiPivot.RotateAround(playerHead.position, Vector3.up, orbitSpeed * Time.deltaTime);
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }
}
