using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;

    [Header("Player References")]
    [Tooltip("The player's head transform (e.g., VR headset or camera).")]
    public Transform playerHead;

    [Header("UI References")]
    [Tooltip("Pivot point for UI rotation around player.")]
    public Transform uiPivot;
    [Tooltip("Canvas object that displays the player's health.")]
    public Canvas healthCanvas;
    [Tooltip("Text element (TMP) inside the canvas showing HP.")]
    public TextMeshProUGUI healthText;

    [Header("UI Settings")]
    public float uiDistance = 1.0f;       // Distance forward from pivot along head direction
    public float pivotYOffset = -0.3f;    // Pivot offset below player's position
    public float orbitSpeed = 0f;         // Optional slow orbit in degrees/sec

    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent onPlayerDied;

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
    }

    // ------------------------------------------------------------
    // Health management
    // ------------------------------------------------------------
    public void DealDamage(float damage)
    {
        if (damage <= 0f) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        onHealthChanged.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            Die();
        }
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
        // TODO: Add player death/respawn handling here
    }

    // ------------------------------------------------------------
    // UI logic
    // ------------------------------------------------------------
    void UpdateUIPivotPosition()
    {
        if (uiPivot == null || playerHead == null) return;

        // Maintain pivot below player’s head horizontally
        Vector3 headPos = playerHead.position;
        uiPivot.position = new Vector3(headPos.x, headPos.y + pivotYOffset, headPos.z);

        // Position canvas in front of pivot, facing same direction as player head
        if (healthCanvas != null)
        {
            Transform canvasTransform = healthCanvas.transform;
            Vector3 forwardDir = playerHead.forward;
            Vector3 targetPos = uiPivot.position + forwardDir * uiDistance;
            canvasTransform.position = targetPos;

            // Make the canvas always face the same direction as the player's head
            canvasTransform.rotation = Quaternion.LookRotation(forwardDir, Vector3.up);
        }
    }

    void RotateUIPivot()
    {
        if (uiPivot == null) return;

        // Optional orbit motion around the player’s head
        if (orbitSpeed != 0f)
        {
            uiPivot.RotateAround(playerHead.position, Vector3.up, orbitSpeed * Time.deltaTime);
        }
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }
}
