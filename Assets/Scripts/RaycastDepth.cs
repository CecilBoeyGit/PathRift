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

    public void TryPlace(RectTransform rect, Camera _centerCam)
    {
        Vector3 rectOrigin = rect.TransformPoint(rect.rect.center);
        var Ray = new Ray(rectOrigin, _centerCam.transform.forward);
        DebugRay(Ray);

        if (raycastManager.Raycast(Ray, out var hit))
        {
            _debugCube.transform.position = hit.point;
        }
        else
        {
            _debugCube.transform.position = Ray.origin;
        }
    }

    void DebugRay(Ray ray)
    {
        Debug.DrawRay(ray.origin, ray.direction * 50, Color.red);
    }
}
