using Meta.XR;
using UnityEngine;
using UnityEngine.UI;

public class RaycastDepth : MonoBehaviour
{

    public EnvironmentRaycastManager raycastManager;
    //public Camera _centerCam;
    public GameObject _debugCube;

/*    public RectTransform _uiRect;
    public GameObject _uiCube;*/

    //public Text DebugText, DebugText02;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public Vector3 TryPlace(RectTransform rect, Transform transformWS, bool useTransformWS, Camera _centerCam)
    {
        Vector3 rectOrigin = Vector3.zero;

        if (useTransformWS)
            rectOrigin = transformWS.position;
        else
            rectOrigin = rect.TransformPoint(rect.rect.center);

        var Ray = new Ray(rectOrigin, _centerCam.transform.forward);
        DebugRay(Ray);

        if (raycastManager.Raycast(Ray, out var hit))
        {
            return hit.point;
        }
        else
        {
            return Ray.origin;
        }
    }

    void DebugRay(Ray ray)
    {
        Debug.DrawRay(ray.origin, ray.direction * 50, Color.red);
    }
}
