using UnityEngine;

public class EnemyStartPosSetting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CheckAndSetYPos(Transform playerPos)
    {
        if (transform.position.y < (playerPos.position.y - 1.2f))
        {
            transform.position = new Vector3(transform.position.x, playerPos.position.y - 1f, transform.position.z);
        }
    }
}
