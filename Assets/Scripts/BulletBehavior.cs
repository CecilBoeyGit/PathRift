using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    Rigidbody rb;
    public ParticleSystem particle;
    private Coroutine lifeRoutine;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        particle.transform.parent = null;
    }

    // Update is called once per frame
    void OnEnable()
    {
        rb.linearVelocity = Vector3.zero;
        lifeRoutine = StartCoroutine(LifeTimer());
    }

    void OnDisable()
    {
        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
        }
    }

    IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(3);
        gameObject.SetActive(false);
    }

    void OnCollisionEnter(Collision other)
    {
        particle.transform.position = this.transform.position;
        this.gameObject.SetActive(false);
    }
}
