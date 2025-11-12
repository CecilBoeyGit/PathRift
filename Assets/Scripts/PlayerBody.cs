using UnityEngine;

public class PlayerBody : MonoBehaviour
{
    CapsuleCollider col;
    public Transform playerHead;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        col = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCollider();
    }
    
    void UpdateCollider()
    {
        transform.position = playerHead.position;

        float height = Mathf.Abs(playerHead.position.y - transform.parent.position.y);
        Vector3 colCenter = col.center;
        colCenter.y = -height / 2f;
        col.center = colCenter; // ✅ apply back
        col.height = height;
    }
}
