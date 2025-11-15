using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyTestSpawner : MonoBehaviour
{
    [Header("Enemy Prefabs (pool per type)")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Area (relative to this spawner)")]
    public float rangeX = 8f;
    public float rangeZ = 4f;

    [Header("Spawn Timing")]
    public float minSpawnDelay = 3f;
    public float maxSpawnDelay = 5f;
    public int maxEnemies = 8;

    // ---------------------------------------------------------
    // Internal pool / active tracking
    // ---------------------------------------------------------
    private List<List<GameObject>> pools;    // pool per enemy type
    private int activeCount = 0;

    [Header("Pool Size Per Enemy Type")]
    public int poolSize = 10;

    void Start()
    {
        InitializePools();
        StartCoroutine(SpawnLoop());
    }

    // ---------------------------------------------------------
    // Create pool for each enemy type
    // ---------------------------------------------------------
    void InitializePools()
    {
        pools = new List<List<GameObject>>();

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            List<GameObject> pool = new List<GameObject>();

            for (int j = 0; j < poolSize; j++)
            {
                GameObject obj = Instantiate(enemyPrefabs[i]);
                obj.SetActive(false);

                // Register callback when enemy gets disabled
                EnemyHealth health = obj.GetComponent<EnemyHealth>();
                if (health != null)
                    health.onUnstunned.AddListener(() => { /* no-op safe */ });

                // Track when the enemy dies (disabled)
                EnemyDeathListener listener = obj.AddComponent<EnemyDeathListener>();
                listener.spawner = this;

                pool.Add(obj);
            }

            pools.Add(pool);
        }
    }

    // ---------------------------------------------------------
    // Main spawn loop
    // ---------------------------------------------------------
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
            TrySpawn();
        }
    }

    // ---------------------------------------------------------
    // Try to spawn (if under maxEnemies)
    // ---------------------------------------------------------
    void TrySpawn()
    {
        if (activeCount >= maxEnemies) return;

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        int index = Random.Range(0, enemyPrefabs.Length);
        GameObject enemy = GetFromPool(index);

        if (enemy == null) return;

        Vector3 pos = GetRandomSpawnPosition();
        enemy.transform.position = pos;
        enemy.transform.rotation = Quaternion.identity;

        enemy.SetActive(true);  // OnEnable resets everything

        activeCount++;
    }

    // ---------------------------------------------------------
    // Get a free enemy from the chosen pool
    // ---------------------------------------------------------
    GameObject GetFromPool(int typeIndex)
    {
        List<GameObject> pool = pools[typeIndex];

        foreach (GameObject obj in pool)
        {
            if (!obj.activeInHierarchy)
                return obj;
        }

        // Pool exhausted → optional: expand pool  
        Debug.LogWarning("Enemy pool exhausted! Consider increasing pool size.");
        return null;
    }

    // ---------------------------------------------------------
    // Random spawn position
    // ---------------------------------------------------------
    Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(-rangeX, rangeX);
        float z = Random.Range(-rangeZ, rangeZ);
        return transform.position + new Vector3(x, 0f, z);
    }

    // ---------------------------------------------------------
    // Called by enemies when they disable (death)
    // ---------------------------------------------------------
    public void NotifyEnemyDisabled()
    {
        activeCount = Mathf.Max(0, activeCount - 1);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(rangeX * 2f, 0.1f, rangeZ * 2f));
    }
#endif
}
