using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy1 : MonoBehaviour
{
    public enum EnemyState { Idle, Aiming, Shooting }

    [Header("References")]
    public Transform player;
    public Transform bodyPivot;
    public Transform firePoint;
    public GameObject bulletPrefab;

    [Header("Settings")]
    public float rotationSpeed = 5f;
    public float minShootDelay = 3f;
    public float maxShootDelay = 5f;
    public float fireRate = 0.2f; // 5 shots per second
    public int minBurstCount = 5;
    public int maxBurstCount = 6;
    public float bulletSpeed = 20f;
    public int poolSize = 20;

    private EnemyState currentState = EnemyState.Idle;
    private Coroutine shootRoutine;
    private List<GameObject> bulletPool;
    private int poolIndex = 0;

    void Start()
    {
        InitializePool();
        ChangeState(EnemyState.Aiming);
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                break;

            case EnemyState.Aiming:
                AimAtPlayer();
                break;

            case EnemyState.Shooting:
                AimAtPlayer();
                break;
        }
    }

    void InitializePool()
    {
        bulletPool = new List<GameObject>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }
    }

    GameObject GetBulletFromPool()
    {
        GameObject bullet = bulletPool[poolIndex];
        poolIndex = (poolIndex + 1) % bulletPool.Count;
        return bullet;
    }

    void AimAtPlayer()
    {
        if (!player || !bodyPivot) return;

        Vector3 playerPos = player.position - new Vector3(0, 0.28f, 0);
        Vector3 direction = (playerPos - bodyPivot.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        bodyPivot.rotation = Quaternion.Slerp(bodyPivot.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                if (shootRoutine != null) StopCoroutine(shootRoutine);
                break;

            case EnemyState.Aiming:
                if (shootRoutine != null) StopCoroutine(shootRoutine);
                shootRoutine = StartCoroutine(ShootTimer());
                break;

            case EnemyState.Shooting:
                if (shootRoutine != null) StopCoroutine(shootRoutine);
                shootRoutine = StartCoroutine(ShootBurst());
                break;
        }
    }

    IEnumerator ShootTimer()
    {
        yield return new WaitForSeconds(Random.Range(minShootDelay, maxShootDelay));
        ChangeState(EnemyState.Shooting);
    }

    IEnumerator ShootBurst()
    {
        int burstCount = Random.Range(minBurstCount, maxBurstCount + 1);

        for (int i = 0; i < burstCount; i++)
        {
            Shoot();
            yield return new WaitForSeconds(fireRate);
        }

        ChangeState(EnemyState.Aiming);
    }

    void Shoot()
    {
        if (!firePoint) return;

        GameObject bullet = GetBulletFromPool();
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;
        bullet.SetActive(true);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = firePoint.forward * bulletSpeed;
        }

        StartCoroutine(DisableAfterSeconds(bullet, 5f)); // Auto-disable bullet after 5s
    }

    IEnumerator DisableAfterSeconds(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet) bullet.SetActive(false);
    }
}
