using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy1Bullet : MonoBehaviour
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
        particle.Play();
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
        particle.Play();

        if (other.gameObject.tag == "Player")
        {
            PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
            playerHealth.DealDamage(12f);
        }
        this.gameObject.SetActive(false);
    }
}
