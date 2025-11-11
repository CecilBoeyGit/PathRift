using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbsController : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform playerHead;
    public Transform idleOrbsPivot;
    public float pivotOffset = 0.4f;
    public float orbFromCenterOffset = 0.5f;
    public Transform[] orbs;
    public List<Transform> orbPool = new List<Transform>();
    List<Orb> orbToChange = new List<Orb>();

    [Header("Orb State References")]
    Orb[] orbScripts; // Assign the same orbs that have Orb scripts attached

    [HideInInspector]
    public Transform target;
    public float maxAngle = 20f;
    public float searchRadius = 50f;
    public LayerMask enemyLayer;
    public LayerMask hitMask = ~0;

    private Collider[] nearbyEnemies = new Collider[50]; // Reused buffer

    public GameObject crystal;
    [HideInInspector]
    public List<GameObject> crystalsPool = new List<GameObject>();

    void Start()
    {
        SetUpOrbs();

        SetUpCrystalsPool();
    }

    void Update()
    {
        MoveOrbs();
        target = FindClosestEnemyInView();
    }

    void SetUpOrbs()
    {
        orbScripts = new Orb[orbs.Length];

        orbPool.Clear();
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null) continue;
            orbScripts[i] = orbs[i].GetComponent<Orb>();
            orbs[i].transform.parent = null;
            orbPool.Add(orbs[i]);
        }
    }
    
    void SetUpCrystalsPool()
    {
        for (int i = 0; i < 30; i++)
        {
            GameObject crystalPrefab = Instantiate(crystal);
            crystalPrefab.SetActive(false);     // keep it inactive until needed
            crystalsPool.Add(crystalPrefab);
        }
    }

    void MoveOrbs()
    {
        if (playerHead == null || idleOrbsPivot == null || orbPool.Count == 0)
            return;

        idleOrbsPivot.position = playerHead.position - new Vector3(0, pivotOffset, 0);
        idleOrbsPivot.Rotate(0, 30 * Time.deltaTime, 0);

        float angleStep = 360f / orbPool.Count;
        Quaternion yawOnly = Quaternion.Euler(0f, idleOrbsPivot.eulerAngles.y, 0f);

        for (int i = 0; i < orbPool.Count; i++)
        {
            float angle = Mathf.Deg2Rad * (angleStep * i);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbFromCenterOffset;

            Vector3 movePos = idleOrbsPivot.position + (yawOnly * offset);
            movePos.y = idleOrbsPivot.position.y;

            orbPool[i].position = Vector3.MoveTowards(orbPool[i].position, movePos, 8 * Time.deltaTime);
        }
    }

    // ---------------- ORB STATE CHECKING SECTION ---------------- //

    /// <summary>
    /// Example event-callable function for first keyword/event.
    /// </summary>
    public void OnKeywordOneTriggered()
    {
        for (int i = 0; i < orbs.Length; i++)
        {
            Orb orbScript = orbs[i].GetComponent<Orb>();
            orbScript.ChangeParticle(0);
        }
        CheckOrbStates("Keyword One");
    }

    /// <summary>
    /// Example event-callable function for second keyword/event.
    /// </summary>
    public void OnKeywordTwoTriggered()
    {
        for (int i = 0; i < orbs.Length; i++)
        {
            Orb orbScript = orbs[i].GetComponent<Orb>();
            orbScript.ChangeParticle(1);
        }
        CheckOrbStates("Keyword Two");
    }

    /// <summary>
    /// Checks each orb's current state and logs which ones are Activated.
    /// </summary>
    private void CheckOrbStates(string source)
    {
        if (orbScripts == null || orbScripts.Length == 0)
        {
            Debug.LogWarning("No Orb scripts assigned in OrbsController.");
            return;
        }

        foreach (Orb orb in orbScripts)
        {
            if (orb == null)
            {
                Debug.LogWarning("An Orb reference is missing.");
                continue;
            }

            if (orb.currentState == Orb.OrbState.Activated)
            {
                orbToChange.Add(orb);
            }
            else
            {
                Debug.Log($"{orb.name} is not Activated (Current state: {orb.currentState}).");
            }
        }
    }

    private Transform FindClosestEnemyInView()
    {
        int count = Physics.OverlapSphereNonAlloc(playerHead.position, searchRadius, nearbyEnemies, enemyLayer);

        if (count == 0)
            return null;

        Transform closest = null;
        float closestSqrDist = Mathf.Infinity;

        Vector3 origin = playerHead.position;
        Vector3 forward = playerHead.forward;
        float minDot = Mathf.Cos(maxAngle * Mathf.Deg2Rad);

        for (int i = 0; i < count; i++)
        {
            Transform enemy = nearbyEnemies[i].transform;
            Vector3 dir = (enemy.position - origin).normalized;

            // Check if within 20 degrees using dot product
            if (Vector3.Dot(forward, dir) >= minDot)
            {
                float sqrDist = (enemy.position - origin).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closest = enemy;
                }
            }
        }

        return closest;
    }

    public Transform GetTarget()
    {
        return target;
    }

    public Vector3 RaycastForward()
    {
        Ray ray = new Ray(playerHead.position, playerHead.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 10, hitMask))
        {
            return hit.point;
        }

        return Vector3.zero; // No hit
    }
}
