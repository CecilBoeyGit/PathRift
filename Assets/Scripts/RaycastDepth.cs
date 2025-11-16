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

        Vector3 fovDirection;
        if(useTransformWS)
            fovDirection = _centerCam.transform.forward;
        else
            fovDirection = rect.gameObject.transform.position - _centerCam.transform.position;

        var Ray = new Ray(rectOrigin, fovDirection);
        DebugRay(Ray);

        if (raycastManager.Raycast(Ray, out var hit))
        {
            return hit.point;
        }
        else
        {
            return Vector3.zero;
        }
    }

    public bool TryGetSize(
    RectTransform rect,
    Transform transformWS,
    bool useTransformWS,
    Camera _centerCam,
    out Vector3 worldPos,
    out Vector2 worldSizeWS)
    {
        Vector3 rectOrigin = Vector3.zero;

        if (useTransformWS)
            rectOrigin = transformWS.position;
        else
            rectOrigin = rect.TransformPoint(rect.rect.center);

        var ray = new Ray(rectOrigin, _centerCam.transform.forward);
        DebugRay(ray);

        if (raycastManager.Raycast(ray, out var hit))
        {
            worldPos = hit.point;

            // Distance from camera to hit point along the ray
            float distance = (hit.point - _centerCam.transform.position).magnitude;

            // Camera frustum size at this distance
            float frustumHeight = 2f * distance * Mathf.Tan(_centerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * _centerCam.aspect;

            // Convert rect size from canvas units to screen pixels
            float scaleFactor = 1f;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas != null)
                scaleFactor = canvas.scaleFactor;

            Vector2 rectScreenSize = rect.rect.size * scaleFactor; // in pixels

            // Normalized size on screen
            float normWidth = rectScreenSize.x / Screen.width;
            float normHeight = rectScreenSize.y / Screen.height;

            // World-space size of the bounding box at this depth
            float worldWidth = frustumWidth * normWidth;
            float worldHeight = frustumHeight * normHeight;

            worldSizeWS = new Vector2(worldWidth, worldHeight);

            return true;
        }
        else
        {
            worldPos = ray.origin;
            worldSizeWS = Vector2.zero;
            return false;
        }
    }


    void DebugRay(Ray ray)
    {
        Debug.DrawRay(ray.origin, ray.direction * 50, Color.red);
    }
}