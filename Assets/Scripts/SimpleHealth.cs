using UnityEngine;

public class SimpleHealth : MonoBehaviour
{
    float health = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Bullet")
        {
            health -= 20;
            if (health <= 0)
            {
                this.gameObject.SetActive(false);
            }
        }
    }
}
