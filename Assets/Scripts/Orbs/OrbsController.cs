using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbsController : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform playerHead;
    public Transform idleOrbsPivot;
    public OrbsHandControl leftHand, rightHand;
    public float pivotOffset = 0.4f;
    public float orbFromCenterOffset = 0.5f;
    public Transform[] orbs;
    public List<Transform> orbPool = new List<Transform>();
    List<Orb> orbToChange = new List<Orb>();

    [Header("Orb State References")]
    Orb[] orbScripts;

    [HideInInspector] public Transform target;
    public float maxAngle = 20f;
    public float searchRadius = 50f;
    public LayerMask enemyLayer;
    public LayerMask hitMask = ~0;
    private Collider[] nearbyEnemies = new Collider[50];

    public GameObject crystal;
    [HideInInspector] public List<GameObject> crystalsPool = new List<GameObject>();
    public GameObject flamePrefab;
    [HideInInspector] public List<GameObject> flamePool = new List<GameObject>();

    // =====================================================
    // ✈️ LOCK-ON HUD SECTION
    // =====================================================
    [Header("Lock-On HUD")]
    public Canvas lockOnCanvas;          // The UI canvas for lock-on (world-space)
    public RectTransform circleUI;       // The ring that represents maxAngle
    public RectTransform lockOnIcon;     // Icon that shows when a target is locked
    public GameObject hitPosIndicator;   // 3D object that hovers at hit position
    public float lockOnDistance = 5f;    // How far in front of playerHead the canvas should appear

    public Color orange, blue;

    private Vector3 currentHitPos = Vector3.zero;
    private Camera mainCam;

    LayerMask ignoreMask;

   [SerializeField]
    private RaycastDepth _depthRaycast;

    // =====================================================

    void Start()
    {
        SetUpOrbs();
        SetUpCrystalsPool();
        SetUpFlamePool();

        mainCam = Camera.main;
        if (lockOnCanvas) lockOnCanvas.enabled = false;
        if (hitPosIndicator) hitPosIndicator.SetActive(false);

        ignoreMask = ~(
        (1 << LayerMask.NameToLayer("Orb"))
        | (1 << LayerMask.NameToLayer("Flame"))
        | (1 << LayerMask.NameToLayer("Crystal"))
        | (1 << LayerMask.NameToLayer("Player"))
        | (1 << LayerMask.NameToLayer("Bullet"))
        | (1 << LayerMask.NameToLayer("EnemyBullet"))
        );
    }

    void Update()
    {
        MoveOrbs();
        target = FindClosestEnemyInView();
        currentHitPos = RaycastForward();

        UpdateLockOnHUD();
    }

    // =====================================================
    // 🌀 ORB LOGIC
    // =====================================================

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
            crystalPrefab.SetActive(false);
            crystalsPool.Add(crystalPrefab);
        }
    }

    void SetUpFlamePool()
    {
        for (int i = 0; i < 20; i++)
        {
            GameObject f = Instantiate(flamePrefab);
            f.SetActive(false);
            flamePool.Add(f);
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

    // =====================================================
    // 🧭 TARGET + RAYCAST SYSTEM
    // =====================================================

    private Transform FindClosestEnemyInView()
    {
        int count = Physics.OverlapSphereNonAlloc(playerHead.position, searchRadius, nearbyEnemies, enemyLayer);
        if (count == 0) return null;

        Vector3 origin  = playerHead.position;
        Vector3 forward = playerHead.forward; // already normalized in Unity, but fine as-is
        float minDot    = Mathf.Cos(maxAngle * Mathf.Deg2Rad); // threshold for FOV

        Transform best = null;
        float bestDot = minDot;                  // only consider candidates with dot >= minDot
        float bestSqrDist = float.PositiveInfinity;

        // Optional: prevent multiple colliders on same enemy from competing
        System.Collections.Generic.HashSet<Transform> seen = new System.Collections.Generic.HashSet<Transform>();

        for (int i = 0; i < count; i++)
        {
            Collider col = nearbyEnemies[i];
            if (!col) continue;

            // Use the root (or attached rigidbody) to represent the enemy uniquely
            Transform enemyRoot = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform.root;
            if (!seen.Add(enemyRoot)) continue;

            // Use closest point on the collider for a fair direction & distance
            Vector3 targetPoint = col.ClosestPoint(origin);
            Vector3 toTarget = (targetPoint - origin);
            float sqrDist = toTarget.sqrMagnitude;
            if (sqrDist < 0.0001f) continue;

            Vector3 dir = toTarget / Mathf.Sqrt(sqrDist); // normalized

            float dot = Vector3.Dot(forward, dir);
            if (dot < minDot) continue; // outside FOV

            // Prefer the largest dot (smallest angle). If nearly equal, pick nearer.
            if (dot > bestDot || (Mathf.Approximately(dot, bestDot) && sqrDist < bestSqrDist))
            {
                best = enemyRoot;
                bestDot = dot;
                bestSqrDist = sqrDist;
            }
        }

        return best;
    }

    public Transform GetTarget() => target;

    private RectTransform emptyRect;    

    public Vector3 RaycastForward()
    {
       // Ray ray = new Ray(playerHead.position, playerHead.forward);

            // if (Physics.Raycast(ray, out RaycastHit hit, 10f, ignoreMask))
            //     return hit.point;
        if(_depthRaycast != null)
        {

            Vector3 worldPos = _depthRaycast.TryPlace(emptyRect, playerHead, true, playerHead.GetComponent<Camera>());
            return worldPos;

        }



        return Vector3.zero;
    }

    public void FireMode()
    {
        var lockOnImg = lockOnIcon.GetComponent<UnityEngine.UI.Image>();
        lockOnImg.color = orange;
        var circleImg = circleUI.GetComponent<UnityEngine.UI.Image>();
        circleImg.color = orange;
        MeshRenderer rend = hitPosIndicator.GetComponent<MeshRenderer>();
        rend.material.SetColor("_BaseColor", orange);

        if (leftHand.activeOrbs.Count > 0)
        {
            foreach (Transform orb in leftHand.activeOrbs)
            {
                Orb orbScript = orb.GetComponent<Orb>();
                orbScript.ChangeParticle(0);
                orbScript.elementState = 0;
            }
        }
        if (rightHand.activeOrbs.Count > 0)
        {
            foreach (Transform orb in rightHand.activeOrbs)
            {
                Orb orbScript = orb.GetComponent<Orb>();
                orbScript.ChangeParticle(0);
                orbScript.elementState = 0;
            }
        }

        if (orbPool.Count > 0)
        {
            foreach (Transform orb in orbPool)
            {
                Orb orbScript = orb.GetComponent<Orb>();
                orbScript.ChangeParticle(0);
                orbScript.elementState = 0;
            }
        }
    }

    public void IceMode()
    {
        var lockOnImg = lockOnIcon.GetComponent<UnityEngine.UI.Image>();
        lockOnImg.color = blue;
        var circleImg = circleUI.GetComponent<UnityEngine.UI.Image>();
        circleImg.color = blue;
        MeshRenderer rend = hitPosIndicator.GetComponent<MeshRenderer>();
        rend.material.SetColor("_BaseColor", blue);

        if (leftHand.activeOrbs.Count > 0)
        {
            foreach (Transform orb in leftHand.activeOrbs)
            {
                Orb orbScript = orb.GetComponent<Orb>();
                orbScript.ChangeParticle(1);
                orbScript.elementState = 1;
            }
        }
        if (rightHand.activeOrbs.Count > 0)
        {
            foreach (Transform orb in rightHand.activeOrbs)
            {
                Orb orbScript = orb.GetComponent<Orb>();
                orbScript.ChangeParticle(1);
                orbScript.elementState = 1;
            }
        }

        if (orbPool.Count > 0)
        {
            foreach (Transform orb in orbPool)
            {
                Orb orbScript = orb.GetComponent<Orb>();
                orbScript.ChangeParticle(1);
                orbScript.elementState = 1;
            }
        }
    }

    // =====================================================
    // ✈️ LOCK-ON HUD UPDATER
    // =====================================================
    private void UpdateLockOnHUD()
    {
        bool anyOrbsActive = (leftHand != null && leftHand.activeOrbs.Count > 0 && leftHand.handState != OrbsHandControl.HandState.ShieldShoot) ||
                            (rightHand != null && rightHand.activeOrbs.Count > 0 && rightHand.handState != OrbsHandControl.HandState.ShieldShoot);

        if (!lockOnCanvas) return;

        // Toggle entire HUD
        lockOnCanvas.enabled = anyOrbsActive;
        if (!anyOrbsActive)
        {
            if (hitPosIndicator) hitPosIndicator.SetActive(false);
            return;
        }

        // Canvas pose: always lockOnDistance ahead, facing the player
        Vector3 canvasPos = playerHead.position + playerHead.forward * lockOnDistance;
        lockOnCanvas.transform.position = canvasPos;
        lockOnCanvas.transform.rotation = Quaternion.LookRotation(playerHead.forward, Vector3.up);

        // --- Correct circle sizing in world-space ---
        if (circleUI)
        {
            // 1) diameter in meters (true physical size)
            float angleRad = maxAngle * Mathf.Deg2Rad;
            float diameterMeters = 2f * Mathf.Tan(angleRad) * lockOnDistance;

            // 2) convert meters -> canvas local units using canvas world scale
            //    (assumes uniform scale; world-space canvases usually are)
            float canvasWorldScale = lockOnCanvas.transform.lossyScale.x;
            float diameterUIUnits = diameterMeters / Mathf.Max(canvasWorldScale, 1e-6f);

            // 3) apply to RectTransform (square)
            circleUI.sizeDelta = new Vector2(diameterUIUnits, diameterUIUnits);
        }

        // Manage lock-on / hitPos icons
        bool hasTarget = target != null;
        Vector3 currentHitPos = RaycastForward();
        bool hasHitPos = !hasTarget && currentHitPos != Vector3.zero;

        if (lockOnIcon) lockOnIcon.gameObject.SetActive(hasTarget);
        if (hitPosIndicator) hitPosIndicator.SetActive(hasHitPos && !lockOnIcon.gameObject.activeInHierarchy);

        // Lock-on icon: place along direction to target at canvas depth
        if (hasTarget && lockOnIcon)
        {
            Vector3 dir = (target.position - playerHead.position).normalized;
            Vector3 worldPos = playerHead.position + dir * lockOnDistance;
            lockOnIcon.position = worldPos; // world pos works for children of world-space canvas
        }

        // Hit-pos indicator: world object hovering at hit point
        if (hasHitPos && hitPosIndicator)
        {
            hitPosIndicator.transform.position = currentHitPos + Vector3.up * 0.1f;
            hitPosIndicator.transform.LookAt(playerHead);
        }
    }
}
