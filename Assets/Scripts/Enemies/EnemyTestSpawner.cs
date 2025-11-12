using UnityEngine;
using System.Collections;

public class EnemyTestSpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject[] enemyPrefabs; // Array of enemy prefabs to choose from
    public string enemyTag = "Enemy"; // Tag used to count total active enemies

    [Header("Spawn Area (relative to spawner)")]
    public float rangeX = 8f;
    public float rangeZ = 4f;

    [Header("Spawn Timing")]
    public float minSpawnDelay = 3f;
    public float maxSpawnDelay = 5f;
    public int maxEnemies = 8;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
            TrySpawnEnemy();
        }
    }

    void TrySpawnEnemy()
    {
        // Check if we're under the max enemy limit
        int currentEnemyCount = GameObject.FindGameObjectsWithTag(enemyTag).Length;
        if (currentEnemyCount >= maxEnemies)
            return;

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        // Pick a random enemy prefab
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (prefab == null) return;

        // Pick a random spawn position relative to this spawner
        float offsetX = Random.Range(-rangeX, rangeX);
        float offsetZ = Random.Range(-rangeZ, rangeZ);
        Vector3 spawnPos = transform.position + new Vector3(offsetX, 0f, offsetZ);

        // Instantiate enemy
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

#if UNITY_EDITOR
    // Visualize spawn area in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(rangeX * 2f, 0.1f, rangeZ * 2f));
    }
#endif
}
