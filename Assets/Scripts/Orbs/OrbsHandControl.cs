using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class OrbsHandControl : MonoBehaviour
{
    public enum HandState
    {
        Normal,
        ShieldShoot
    }
    public HandState handState;

    public GameObject bulletPrefab;
    public int bulletPoolSize = 50;
    public float fireRate = 0.08f;
    List<GameObject> bulletPool;

    public GameObject shield;

    bool shootMode = false;

    public bool rightHand = false;
    GestureDetect gestureDetect;
    public OrbsController orbsController;

    public float hoverHeight = 0.2f;
    public float hoverRadius = 0.1f;
    public Transform hoverPivot;
    float roll;
    int orbsNum;

    [Header("References")]
    public Transform centerEye;

    [Header("Settings")]
    public float speedThreshold = 0.5f;
    public float durationThreshold = 0.15f;

    [Header("Event")]
    public UnityEvent onConditionMet;

    private Vector3 lastPosition;
    private float timeAboveThreshold = 0f;
    private bool trackingMovement = false;
    private float startDistanceToCenterEye = 0f;
    private float lastThrowTime = -999f; // tracks last activation time
    private float throwCooldown = 0.5f;

    Vector3 handUpAxis;
    Vector3 handForwardAxis;
    Vector3 handSideAxis;

    public List<Transform> activeOrbs = new List<Transform>();

    // Start is called before the first frame update
    void Start()
    {
        lastPosition = Vector3.zero;
        shield.SetActive(false);
        bulletPool = new List<GameObject>();
        for (int i = 0; i < bulletPoolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);     // keep it inactive until needed
            bulletPool.Add(bullet);
        }

        gestureDetect = GetComponent<GestureDetect>();
        //orbsController = transform.root.GetComponent<OrbsController>();
        handState = HandState.Normal;
    }

    // Update is called once per frame
    void Update()
    {
        // if (!gestureDetect.hasStarted)
        // {
        //     return;
        // }
        GetUpAxis();

        switch (handState)
        {
            case HandState.Normal:

                ShieldShootActivated();
                UpdateOrbs();
                Throw();

                break;

            case HandState.ShieldShoot:

                ShieldShoot();

                break;
        }
    }

    void GetUpAxis()
    {
        if (gestureDetect.rightHand)
        {
            handUpAxis = transform.forward;
        }
        else
        {
            handUpAxis = transform.forward;
        }

        if (gestureDetect.rightHand)
        {
            handForwardAxis = -transform.up;
        }
        else
        {
            handForwardAxis = -transform.up;
        }

        if (gestureDetect.rightHand)
        {
            handSideAxis = transform.right;
        }
        else
        {
            handSideAxis = transform.right;
        }
    }

    void GetActiveOrbs()
    {
        if (orbsNum - activeOrbs.Count > 0 && orbsController.orbPool.Count > 0)
        {
            Debug.Log("OrbsNum is greater");
            int orbsToActivate = 0;

            if (orbsController.orbPool.Count >= (orbsNum - activeOrbs.Count))
            {
                orbsToActivate = orbsNum - activeOrbs.Count;
            }
            else
            {
                orbsToActivate = orbsController.orbPool.Count;
            }

            for (int i = 0; i < orbsToActivate; i++)
            {
                if (orbsController.orbPool.Count == 0)
                {
                    break;
                }
                Transform orbTransform = null;
                float minDist = 500f;

                if (orbsToActivate > 1)
                {
                    for (int j = 0; j < orbsController.orbPool.Count; j++)
                    {
                        float dist = Vector3.Distance(orbsController.orbPool[j].position, transform.position);
                        if (dist < minDist)
                        {
                            orbTransform = orbsController.orbPool[j];
                            minDist = dist;
                        }
                    }
                }
                else
                {
                    orbTransform = orbsController.orbPool[0];
                }

                if (orbTransform != null)
                {
                    orbTransform.GetComponent<Orb>().currentState = Orb.OrbState.Activated;
                    activeOrbs.Add(orbTransform);
                    orbsController.orbPool.Remove(orbTransform);

                }
                
                //arm.GetComponent<ArmScript>().currentState = ArmScript.ArmState.Activated;
            }
        }

        else if ((orbsNum - activeOrbs.Count) < 0)
        {
            int num = activeOrbs.Count - orbsNum;

            for (int i = 0; i < (activeOrbs.Count - orbsNum); i++)
            {
                Transform thisOrb = activeOrbs[i];
                orbsController.orbPool.Add(thisOrb);
                activeOrbs.Remove(thisOrb);
                thisOrb.GetComponent<Orb>().currentState = Orb.OrbState.Idle;
                thisOrb.GetComponent<Orb>().activatedPos = null;
            }
        }
    }

    void UpdateOrbs()
    {
        if (handState != HandState.Normal)
        {
            return;
        }
        OrbsNum();
        GetActiveOrbs();

        hoverPivot.position = transform.position + handUpAxis * hoverHeight;
        hoverPivot.rotation = Quaternion.LookRotation(hoverPivot.position - transform.position);
        if (activeOrbs.Count == 1)
        {
            activeOrbs[0].position = Vector3.MoveTowards(activeOrbs[0].position, hoverPivot.position, 5f * Time.deltaTime);
            activeOrbs[0].rotation = Quaternion.RotateTowards(activeOrbs[0].rotation, hoverPivot.rotation, 800 * Time.deltaTime);
        }
        else if (activeOrbs.Count > 1)
        {
            float angles = 360 / activeOrbs.Count;

            for (int i = 0; i < activeOrbs.Count; i++)
            {
                float angle = i * angles;
                Vector3 distOffset = (Quaternion.AngleAxis(angle, hoverPivot.forward) * hoverPivot.right).normalized * hoverRadius;

                activeOrbs[i].position = Vector3.MoveTowards(activeOrbs[i].position, hoverPivot.position + distOffset, 5f * Time.deltaTime);
                activeOrbs[i].rotation = Quaternion.RotateTowards(activeOrbs[i].rotation, hoverPivot.rotation, 800 * Time.deltaTime);
            }
        }
    }

    private void Throw()
    {
        // Cooldown check
        if (Time.time - lastThrowTime < throwCooldown)
            return;

        if (activeOrbs.Count < 1)
            return;

        // Calculate velocity magnitude
        float velocity = (transform.position - lastPosition).magnitude / Time.deltaTime;

        if (velocity > speedThreshold)
        {
            // Start tracking if this is the first frame above threshold
            if (!trackingMovement)
            {
                trackingMovement = true;
                timeAboveThreshold = 0f;
                startDistanceToCenterEye = Vector3.Distance(transform.position, centerEye.position);
            }

            timeAboveThreshold += Time.deltaTime;

            // If maintained high speed for enough time
            if (timeAboveThreshold >= durationThreshold)
            {
                float endDistance = Vector3.Distance(transform.position, centerEye.position);
                Vector3 hitPoint = orbsController.RaycastForward();

                if (endDistance > startDistanceToCenterEye)
                {
                    Transform targetToTrack = orbsController.GetTarget();
                    int activeOrbsNum = activeOrbs.Count;

                    if (targetToTrack != null)
                    {
                        for (int i = 0; i < activeOrbs.Count; i++)
                        {
                            Orb orbScript = activeOrbs[i].GetComponent<Orb>();
                            orbScript.target = targetToTrack;
                            orbScript.orbsLaunched = activeOrbsNum;
                            orbScript.currentState = Orb.OrbState.Attack;
                        }

                        activeOrbs.Clear();
                        trackingMovement = false;
                        lastThrowTime = Time.time; // ✅ start cooldown timer
                        return;
                    }

                    if (hitPoint != Vector3.zero)
                    {
                        for (int i = 0; i < activeOrbs.Count; i++)
                        {
                            Orb orbScript = activeOrbs[i].GetComponent<Orb>();
                            orbScript.hitPos = hitPoint;
                            orbScript.orbsLaunched = activeOrbsNum;
                            orbScript.currentState = Orb.OrbState.Attack;
                        }

                        activeOrbs.Clear();
                        trackingMovement = false;
                        lastThrowTime = Time.time; // ✅ start cooldown timer
                        return;
                    }
                }
            }
        }
        else
        {
            // Reset if speed drops below threshold
            trackingMovement = false;
            timeAboveThreshold = 0f;
        }

        lastPosition = transform.position;
    }


    void ShieldShootActivated()
    {
        if (gestureDetect.fingerRotState[1] == 2 && gestureDetect.fingerRotState[2] < 2 && gestureDetect.fingerRotState[3] < 2 && gestureDetect.fingerRotState[4] == 2 && gestureDetect.fingerRotState[0] != 1 && orbsController.orbPool.Count >= 2)
        {
            // for (int i = 0; i < activeOrbs.Count; i++)
            // {
            //     Transform thisOrb = activeOrbs[i];
            //     orbsController.orbPool.Add(thisOrb);
            //     activeOrbs.Remove(thisOrb);
            // }
            // for (int i = 0; i < 2; i++)
            // {
            //     Transform thisOrb = orbsController.orbPool[i];
            //     orbsController.orbPool.Remove(thisOrb);
            //     activeOrbs.Add(thisOrb);
            // }

            orbsNum = 2;
            GetActiveOrbs();

            handState = HandState.ShieldShoot;
            return;
        }
    }

    void ShieldShoot()
    {
        //Debug.Log("ShieldSHoot Activated");
        if (gestureDetect.fingerRotState[1] < 2 || gestureDetect.fingerRotState[2] == 2 || gestureDetect.fingerRotState[3] == 2 || gestureDetect.fingerRotState[4] < 2)
        {
            shootMode = false;
            handState = HandState.Normal;
            shield.SetActive(false);

            return;
        }

        if (gestureDetect.fingerRotState[0] > 1)
        {
            shootMode = false;

        }
        else if (gestureDetect.fingerRotState[0] < 1)
        {
            if (!shootMode)
            {
                shootMode = true;
                StartCoroutine(Shoot());
            }

        }
        //Debug.Log("Shoot Mode: " + shootMode);

        // Vector3 offsetDir = Quaternion.AngleAxis(-41f, handSideAxis) * handForwardAxis;
        // Vector3 offsetDir2 = Quaternion.AngleAxis(-4f, handUpAxis) * handForwardAxis;
        // // hoverPivot.position = transform.position + offsetDir.normalized * hoverHeight;
        // hoverPivot.position = transform.position + offsetDir * offsetDir2 * hoverHeight;

        float sideRotationValue = 4;
        if (!rightHand)
        {
            sideRotationValue *= -1;
        }
        Quaternion combinedRotation =
        Quaternion.AngleAxis(sideRotationValue, handUpAxis) *
        Quaternion.AngleAxis(-38f, handSideAxis);

        Vector3 offsetDir = combinedRotation * handForwardAxis;

        hoverPivot.position = transform.position + offsetDir * hoverHeight;


        if (!shootMode)
        {
            shield.SetActive(true);
            hoverPivot.rotation = Quaternion.LookRotation((transform.position - hoverPivot.position));

            shield.transform.rotation = hoverPivot.rotation;
            shield.transform.position = hoverPivot.position;
        }
        else
        {
            shield.SetActive(false);
            Quaternion lookRot = Quaternion.LookRotation((transform.position - hoverPivot.position));
            roll += 300 * Time.deltaTime;

            hoverPivot.rotation = lookRot * Quaternion.Euler(0, 0, roll);
        }

        float angles = 360 / activeOrbs.Count;
        for (int i = 0; i < activeOrbs.Count; i++)
        {
            float angle = i * angles;
            Vector3 distOffset = (Quaternion.AngleAxis(angle, hoverPivot.forward) * hoverPivot.right).normalized * hoverRadius;
            activeOrbs[i].position = Vector3.MoveTowards(activeOrbs[i].position, hoverPivot.position + distOffset, 6f * Time.deltaTime);
            activeOrbs[i].rotation = Quaternion.RotateTowards(activeOrbs[i].rotation, hoverPivot.rotation, 800 * Time.deltaTime);
        }
    }

    IEnumerator Shoot()
    {
        while (shootMode && handState == HandState.ShieldShoot)
        {
            GameObject bullet = bulletPool[0];
            bulletPool.RemoveAt(0);
            bulletPool.Add(bullet);
            bullet.transform.position = hoverPivot.position + -hoverPivot.forward * 0.1f;
            bullet.transform.rotation = hoverPivot.rotation * Quaternion.Euler(0, 180, 0);
            bullet.SetActive(true);

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.AddForce(bullet.transform.forward * 20, ForceMode.Impulse);

            yield return new WaitForSeconds(fireRate);
        }
    }

    void OrbsNum()
    {
        orbsNum = 0;

        if (gestureDetect.fingerRotState[1] == 2)
        {
            orbsNum++;
        }

        if (gestureDetect.fingerRotState[2] == 2)
        {
            orbsNum++;
        }

        if (gestureDetect.fingerRotState[3] == 2)
        {
            orbsNum++;
        }

        if (gestureDetect.fingerRotState[4] == 2)
        {
            orbsNum++;
        }

        if (gestureDetect.fingerRotState[0] > 0)
        {
            orbsNum = 0;
        }
    }
    
    private void ReturnToIdle(Transform orb)
    {
        orbsController.orbPool.Add(orb);
    }
}
