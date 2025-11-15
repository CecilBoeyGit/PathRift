using UnityEngine;

public class EnemyDeathListener : MonoBehaviour
{
    [HideInInspector] 
    public EnemyTestSpawner spawner;

    void OnDisable()
    {
        // Notify spawner only if pooling is active (OnDisable is NOT called when exiting play mode)
        if (spawner != null && this.gameObject.activeInHierarchy == false)
        {
            spawner.NotifyEnemyDisabled();
        }
    }
}
