using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

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
        // TODO: Add explosion, animation, or pooling logic here
        Debug.Log($"{gameObject.name} has died.");
        //gameObject.SetActive(false); // temporary pooling-safe disable

        Destroy(this.gameObject);
    }
}
