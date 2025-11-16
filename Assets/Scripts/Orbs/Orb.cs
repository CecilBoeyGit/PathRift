using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour
{
    AudioSource audioS;
    Collider col;
    bool activated = false, collided = false;

    public Transform activatedPos, target;
    public Vector3 hitPos;
    public ParticleSystem[] particleEffects;
    public OrbsController orbsController;
    public MeshRenderer orbRend;
    public Color[] colors;

    public float orbsLaunched, speed = 8;

    int enemyLayer;
    public int elementState = 0; // 0 = neutral, 1 = crystal element, etc.

    public enum OrbState
    {
        Idle,
        Activated,
        Attack,
        Return,
        Attacking,
        Tracing,
        Shield,
        Spear
    }

    public OrbState currentState;
    [HideInInspector] public GameObject targetTransform;

    void Update()
    {
        switch (currentState)
        {
            case OrbState.Attack:
                OrbsThrown();
                break;

            case OrbState.Return:
                ReturnOrb();
                break;
        }
    }

    void OrbsThrown()
    {
        if ((target == null && hitPos == Vector3.zero) || collided)
        {
            collided = false;
            currentState = OrbState.Return;
            return;
        }

        if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.05f || target == null)
            {
                // Only spawn crystals if elementState == 1
                if (elementState == 0)
                    CreateFlamesRadial(transform.position);
                if (elementState == 1)
                    CreateCrystalsRadial(transform.position);

                target = null;
                currentState = OrbState.Return;
            }
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, hitPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, hitPos) < 0.1f)
        {
            // Only spawn crystals if elementState == 1
            if (elementState == 0)
                CreateFlame();
            if (elementState == 1)
                CreateCrystals();

            hitPos = Vector3.zero;
            currentState = OrbState.Return;
        }
    }

    void ReturnOrb()
    {
        transform.position = Vector3.MoveTowards(transform.position, orbsController.idleOrbsPivot.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, orbsController.idleOrbsPivot.position) < 1.5f)
        {
            orbsController.orbPool.Add(this.transform);
            currentState = OrbState.Idle;
        }
    }

    public void ChangeParticle(int index)
    {
        foreach (ParticleSystem part in particleEffects)
            part.Stop();

        elementState = index; // update the current element mode
        particleEffects[index].Play();
        orbRend.material.SetColor("_EmissionColor", colors[index]);
    }

    // -------------------- FIRE FUNCTIONS -------------------- //

    void CreateFlame()
    {
        int spawnCount = Random.Range(1, 3);  // spawn 1–2 flames (tweak as needed)

        for (int i = 0; i < spawnCount; i++)
        {
            // Pool management — grab from front, recycle to back
            GameObject flame = orbsController.flamePool[0];
            orbsController.flamePool.RemoveAt(0);
            orbsController.flamePool.Add(flame);

            // Base position
            flame.transform.position = hitPos;

            // --- World-space positional offset ---
            float offsetDist = Random.Range(0.6f, 1.2f);
            float offsetAngle = Random.Range(0f, 360f);

            Vector3 offsetDir = new Vector3(
                Mathf.Cos(offsetAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(offsetAngle * Mathf.Deg2Rad)
            );

            flame.transform.position += offsetDir * offsetDist;

            // --- Uniform scale multiplier (XYZ all equal) ---
            float scaleMult = Random.Range(1.2f, 1.6f);
            Vector3 newScale = Vector3.one * scaleMult * (1f + 0.25f * orbsLaunched);

            // If you use a FlameBehavior with SetTargetScale:
            FlameBehavior fb = flame.GetComponent<FlameBehavior>();
            if (fb != null)
            {
                fb.SetTargetScale(newScale);
                fb.SetDamageMult(1 + 0.2f * orbsLaunched);
            }
            else
                flame.transform.localScale = newScale;
            
            flame.SetActive(true);
        }
    }
    
    void CreateFlamesRadial(Vector3 spawnPos)
    {
        int spawnCount = Random.Range(3, 5); // 3–4 flames (adjust as needed)

        for (int i = 0; i < spawnCount; i++)
        {
            // Pool management — grab from front, recycle to back
            GameObject flame = orbsController.flamePool[0];
            orbsController.flamePool.RemoveAt(0);
            orbsController.flamePool.Add(flame);

            // Base position
            flame.transform.position = spawnPos;
            flame.SetActive(true);

            // --- Radial offset (XZ plane), 0.6–0.9m ---
            float offsetDist = Random.Range(0.6f, 0.9f);
            float offsetAngle = Random.Range(0f, 360f);

            Vector3 dir = new Vector3(
                Mathf.Cos(offsetAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(offsetAngle * Mathf.Deg2Rad)
            ).normalized;

            flame.transform.position += dir * offsetDist;

            // --- Uniform scaling (XYZ) ---
            float scaleMult = Random.Range(1.2f, 1.6f);
            Vector3 newScale = Vector3.one * scaleMult * (1f + 0.3f * (orbsLaunched - 1));

            // Assign scale properly through your flame behavior script
            FlameBehavior fb = flame.GetComponent<FlameBehavior>();
            if (fb != null)
            {
                fb.SetTargetScale(newScale);
                fb.SetDamageMult(1 + 0.3f * (orbsLaunched - 1));
            }
                
            else
                flame.transform.localScale = newScale;
        }
    }



    // -------------------- CRYSTAL FUNCTIONS -------------------- //

    void CreateCrystals()
    {
        int spawnCount = Random.Range(2, 4);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject crystal = orbsController.crystalsPool[0];
            orbsController.crystalsPool.RemoveAt(0);
            orbsController.crystalsPool.Add(crystal);

            crystal.transform.position = hitPos;
            crystal.transform.up = Vector3.up;

            float offsetDist = Random.Range(0.2f, 0.4f);
            float offsetAngle = Random.Range(0f, 360f);
            Vector3 offsetDir = new Vector3(Mathf.Cos(offsetAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(offsetAngle * Mathf.Deg2Rad));
            crystal.transform.position += offsetDir * offsetDist;

            float tiltX = Random.Range(-8f, 8f);
            float tiltZ = Random.Range(-8f, 8f);
            Quaternion tiltRot = Quaternion.Euler(tiltX, 0f, tiltZ);
            crystal.transform.rotation = tiltRot * crystal.transform.rotation;

            Vector3 newScale = crystal.transform.localScale;
            newScale.x = Random.Range(0.8f, 1.3f);
            newScale.z = Random.Range(0.8f, 1.3f);
            newScale.y = Random.Range(0.85f, 1.5f);
            newScale *= (1 + 0.25f * orbsLaunched);

            CrystalBehavior cb = crystal.GetComponent<CrystalBehavior>();
            cb.SetTargetScale(newScale);
            cb.SetStunMult((1 + 0.2f * orbsLaunched) * Random.Range(0.9f, 1.05f));

            crystal.SetActive(true);
        }
    }

    void CreateCrystalsRadial(Vector3 spawnPos)
    {
        int spawnCount = Random.Range(4, 6);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject crystal = orbsController.crystalsPool[0];
            orbsController.crystalsPool.RemoveAt(0);
            orbsController.crystalsPool.Add(crystal);

            crystal.transform.position = spawnPos;
            crystal.SetActive(true);

            float offsetAngle = Random.Range(0f, 360f);
            Vector3 dir = new Vector3(Mathf.Cos(offsetAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(offsetAngle * Mathf.Deg2Rad)).normalized;

            float offsetDist = Random.Range(0.15f, 0.35f);
            crystal.transform.position += dir * offsetDist;

            Vector3 outwardUp = (dir + Vector3.up * Random.Range(0.2f, 0.5f)).normalized;
            crystal.transform.up = outwardUp;
            crystal.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);

            Vector3 newScale = crystal.transform.localScale;
            newScale.x = Random.Range(0.8f, 1.3f);
            newScale.z = Random.Range(0.8f, 1.3f);
            newScale.y = Random.Range(1.2f, 1.8f);
            newScale *= (1 + 0.25f * orbsLaunched);

            CrystalBehavior cb = crystal.GetComponent<CrystalBehavior>();
            cb.SetTargetScale(newScale);
            cb.SetStunMult((1 + 0.2f * orbsLaunched) * Random.Range(0.9f, 1.05f));
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (currentState == OrbState.Attack && other.gameObject.tag != gameObject.tag)
        {
            if (other.gameObject.tag != "Enemy")
                collided = true;
        }
    }
}
