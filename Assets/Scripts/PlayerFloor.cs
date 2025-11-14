using UnityEngine;

public class PlayerFloor : MonoBehaviour
{
    public Transform playerHead, trackingSpace;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(playerHead.position.x, trackingSpace.position.y, playerHead.position.z);
    }
}
