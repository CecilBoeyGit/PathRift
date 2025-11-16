using UnityEngine;
using UnityEngine.Events;

public class PinchDetection : MonoBehaviour
{
    public Transform centerEye;
    public GestureDetect gestureScript;
    public GameObject path;
    public UnityEvent callPinch;
    bool pinched = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(gestureScript.fingerbones[5].Transform.position, gestureScript.fingerbones[10].Transform.position) <= 0.015f && (centerEye.position.y - gestureScript.gameObject.transform.position.y) < 0.3f)
        {
            if (!pinched)
            {
                callPinch.Invoke();
                pinched = true;
            }
        }
        else if (Vector3.Distance(gestureScript.fingerbones[5].Transform.position, gestureScript.fingerbones[10].Transform.position) > 0.03f)
        {
            pinched = false;
        }
    }
}
