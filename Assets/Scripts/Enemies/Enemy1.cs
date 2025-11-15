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

    // --- stun handling ---
    public ParticleSystem stunParticle;
    private bool isStunned = false;

    void Start()
    {
        SetInitialRot();
        InitializePool();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        ChangeState(EnemyState.Aiming);
    }

    void SetInitialRot()
    {
        transform.rotation = Quaternion.LookRotation(player.position - transform.position);
    }

    void Update()
    {
        if (isStunned) return; // skip AI logic while stunned

        switch (currentState)
        {
            case EnemyState.Idle:
                break;

            case EnemyState.Aiming:
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

        if (shootRoutine != null)
            StopCoroutine(shootRoutine);

        switch (newState)
        {
            case EnemyState.Aiming:
                shootRoutine = StartCoroutine(ShootTimer());
                break;

            case EnemyState.Shooting:
                shootRoutine = StartCoroutine(ShootBurst());
                break;
        }
    }

    IEnumerator ShootTimer()
    {
        yield return new WaitForSeconds(Random.Range(minShootDelay, maxShootDelay));
        if (!isStunned) ChangeState(EnemyState.Shooting);
    }

    IEnumerator ShootBurst()
    {
        int burstCount = Random.Range(minBurstCount, maxBurstCount + 1);

        for (int i = 0; i < burstCount; i++)
        {
            if (isStunned) yield break;
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

        StartCoroutine(DisableAfterSeconds(bullet, 5f));
    }

    IEnumerator DisableAfterSeconds(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet) bullet.SetActive(false);
    }

    // --------------------------------------------------------------------
    // UNITY EVENT HOOKS (plug into EnemyHealth events)
    // --------------------------------------------------------------------
    public void OnStunned()
    {
        if (isStunned) return;
        isStunned = true;
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);

        stunParticle.Play();
        ChangeState(EnemyState.Idle);
        Debug.Log($"{name} stunned — attack disabled");
    }

    public void OnUnstunned()
    {
        if (!isStunned) return;
        isStunned = false;
        stunParticle.Stop();
        ChangeState(EnemyState.Aiming);
        Debug.Log($"{name} recovered from stun — attack resumed");
    }

    void OnEnable()
    {
        // Reset stun state
        isStunned = false;

        // Stop stun VFX
        if (stunParticle != null)
            stunParticle.Stop();

        // Reset bullet pool index
        poolIndex = 0;

        // Reset state
        currentState = EnemyState.Idle;

        // Stop any leftover coroutine
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);
        shootRoutine = null;

        // Reset rotation and player reference
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        SetInitialRot();

        // Restart AI
        ChangeState(EnemyState.Aiming);
    }

    void OnDisable()
    {
        // Stop coroutines on disable
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);
        shootRoutine = null;

        // Stop stun effects if active
        if (stunParticle != null)
            stunParticle.Stop();
    }

}
