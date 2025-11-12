using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour
{
    AudioSource audioS;
    //public AudioClip swordHitSound;
    //public TwoHandScript twohand;
    Collider col;
    //public PlayerManager playerScript;
    bool activated = false, collided = false;
    public Transform activatedPos, target;
    public Vector3 hitPos;
    public ParticleSystem[] particleEffects;
    public OrbsController orbsController;

    public float delayTime = 0, swordDelay, speed = 8;

    int enemyLayer;

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
    [HideInInspector]
    public GameObject targetTransform;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case OrbState.Idle:

                break;

            case OrbState.Activated:

                break;

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
                target = null;
                currentState = OrbState.Return;
            }
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, hitPos, speed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, hitPos) < 0.1f)
        {
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
        {
            part.Stop();
        }

        particleEffects[index].Play();
    }

    void CreateCrystals()
    {
        int spawnCount = Random.Range(2, 4);

        for (int i = 0; i < spawnCount; i++)
        {
            // Pool management — grab from front, recycle to back
            GameObject crystal = orbsController.crystalsPool[0];
            orbsController.crystalsPool.RemoveAt(0);
            orbsController.crystalsPool.Add(crystal);

            // Position and activate
            crystal.transform.position = hitPos;
            crystal.SetActive(true);

            // Align 'up' with the normal (vertical orientation)
            crystal.transform.up = Vector3.up;

            // Apply random positional offset BEFORE rotation
            float offsetDist = Random.Range(0.2f, 0.4f);
            float offsetAngle = Random.Range(0f, 360f);
            Vector3 offsetDir = new Vector3(Mathf.Cos(offsetAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(offsetAngle * Mathf.Deg2Rad));
            crystal.transform.position += offsetDir * offsetDist;

            // Apply small random rotation offset (3–12 degrees)
            float tiltX = Random.Range(-8f, 8f); // degrees
            float tiltZ = Random.Range(-8f, 8f); // degrees
            Quaternion tiltRot = Quaternion.Euler(tiltX, 0f, tiltZ);
            crystal.transform.rotation = tiltRot * crystal.transform.rotation;

            // Random scale
            Vector3 newScale = crystal.transform.localScale;
            newScale.x = Random.Range(0.65f, 1.1f);
            newScale.z = Random.Range(0.65f, 1.1f);
            newScale.y = Random.Range(0.7f, 1.3f);
            crystal.transform.localScale = newScale;
        }
    }


    void OnCollisionEnter(Collision other)
    {
        if (currentState == OrbState.Attack && other.gameObject.tag != this.gameObject.tag)
        {
            if (other.gameObject.tag == "Enemy")
            {

            }
            else
            {
                // Vector3 contactNormal = other.contacts[0].normal;
                // Vector3 contactPoint = other.contacts[0].point;

                // // Number of crystals to spawn (2–3)
                // int spawnCount = Random.Range(2, 4);

                // for (int i = 0; i < spawnCount; i++)
                // {
                //     // Pool management — grab from front, recycle to back
                //     GameObject crystal = orbsController.crystalsPool[0];
                //     orbsController.crystalsPool.RemoveAt(0);
                //     orbsController.crystalsPool.Add(crystal);

                //     // Position and activate
                //     crystal.transform.position = contactPoint;
                //     crystal.SetActive(true);

                //     // Align 'up' with the normal
                //     crystal.transform.up = contactNormal;

                //     // Apply small random rotation offset (3–12 degrees)
                //     float randomRot = Random.Range(3f, 12f);
                //     crystal.transform.Rotate(contactNormal, randomRot, Space.World);

                //     // Random scale
                //     Vector3 newScale = crystal.transform.localScale;
                //     newScale.x = Random.Range(0.8f, 1.3f);
                //     newScale.z = Random.Range(0.8f, 1.3f);
                //     newScale.y = Random.Range(1.3f, 1.9f);
                //     crystal.transform.localScale = newScale;
                // }
            }

            collided = true;
        }
    }
}
