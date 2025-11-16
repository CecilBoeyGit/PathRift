using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;

public class LineRendererCreator : MonoBehaviour
{
    private IA_Debug _InputActions_Debug;

    [SerializeField]
    private Camera _centerCam;

    [SerializeField]
    private RaycastDepth _depthRaycast;

    [SerializeField]
    private LayerMask contactObjectLayer;
    [SerializeField]
    private float initialHeightOffset = 5.0f;
    [SerializeField]
    private float overallHeightOffset = 0.0f;

    [SerializeField]
    private GameObject playerTracking;

    [SerializeField]
    private GameObject spherePrefab;

    private Material _pathMaterial;

    [SerializeField]
    private Color contactColor;
    [SerializeField]
    private Color uncontactedColor;
    public static GameObject contactObject = null;
    private GameObject previousContactObject = null;

    public Transform markersParent;

    LineRenderer _lineMain;

    private Vector3 _initialPosition;
    private RectTransform _rectPlaceholder;

    Coroutine CO_LerpPath;

    [Header("DEBUG")]
    [SerializeField]
    private bool useDebugObject = false;

    // Internal storage: positions in the SAME space the LineRenderer uses.
    private readonly List<Vector3> _points = new List<Vector3>();
    private readonly List<Vector3> _initialPoints = new List<Vector3>();
    private readonly List<GameObject> _markers = new List<GameObject>();
    private readonly List<Vector3> _initialMarkers = new List<Vector3>();

    public static LineRendererCreator instance;

    private void Awake()
    {
        if (LineRendererCreator.instance == null)
            instance = this;

        _InputActions_Debug = new IA_Debug();

        _lineMain = GetComponent<LineRenderer>();
        if (_lineMain == null)
            return;
    }

    private void OnEnable()
    {
        if (useDebugObject)
        {
            _InputActions_Debug.DefaultMap.Enable();
            _InputActions_Debug.DefaultMap.Debug_01.performed += Debug_01_Called;
            _InputActions_Debug.DefaultMap.Debug_02.performed += Debug_02_Called;
        }

        if (CO_LerpPath != null)
            StopCoroutine(CO_LerpPath);

        CO_LerpPath = StartCoroutine(InterpolatePathColor(1.0f, false));

        // --- Listens and subscribes to hand gesture events ---
        // += AddPoint_Called
        // += DeletePoint_Called
    }

    private void OnDisable()
    {
        if (useDebugObject)
        {
            _InputActions_Debug.DefaultMap.Debug_01.performed -= Debug_01_Called;
            _InputActions_Debug.DefaultMap.Debug_02.performed -= Debug_02_Called;
            _InputActions_Debug.DefaultMap.Disable();
        }

        if (CO_LerpPath != null)
            StopCoroutine(CO_LerpPath);

        // --- Unsubscrible to hand gesture events ---
        // -= AddPoint_Called
        // -= DeletePoint_Called

        StartGame();

    }

    public void StartGame()
    {
        foreach(GameObject child in _markers)
        {
            child.SetActive(false);    
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _initialPosition = transform.position;
        _pathMaterial = _lineMain.material;
    }

    IEnumerator InterpolatePathColor(float time, bool inverseLerp)
    {
        float timer = 0;
        while(timer < time)
        {
            timer += Time.deltaTime;
            if(_pathMaterial != null)
            {
                if(inverseLerp)
                    _pathMaterial.SetFloat("_NormalizedLerp", 1 - timer / time);
                else
                    _pathMaterial.SetFloat("_NormalizedLerp", timer / time);
            }

            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < _points.Count; i++)
        {
            _points[i] = _initialPoints[i] + new Vector3(0, overallHeightOffset, 0);
            _markers[i].transform.position = _initialMarkers[i] + new Vector3(0, overallHeightOffset, 0);
        }
        _lineMain.SetPositions(_points.ToArray());

        Vector3 rayPos = useDebugObject ? playerTracking.transform.position : _centerCam.transform.position;
        Vector4 rayDirection = useDebugObject ? playerTracking.transform.forward : _centerCam.transform.forward;

        Debug.DrawRay(rayPos, rayDirection * 10f, Color.red);

        Ray eyeRay = new Ray(rayPos, rayDirection);
        RaycastHit hit;
        if (Physics.Raycast(eyeRay, out hit, 100f, contactObjectLayer))
        {
            SetContactObject(hit.transform.gameObject);
        }
        else
        {
            SetContactObject(null);
        }
    }
    void AddPoint_Called()
    {
        print("Path Point Added! ---");
        AddPoint();
    }
    void DeletePoint_Called()
    {
        print("Path Point Deleted! ---");
        DeleteAtContactObject();
    }

    #region --- DEBUG FUNCTIONS ---
    void Debug_01_Called(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            print("Debug_01: Keyboard A triggered! ---");
            AddPoint();
        }
    }
    void Debug_02_Called(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            print("Debug_02: Keyboard D triggered! ---");
            DeleteAtContactObject();
        }
    }
    #endregion

    void SetContactObject(GameObject obj)
    {
        if(previousContactObject != null)           
            previousContactObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", uncontactedColor);

        contactObject = obj;
        previousContactObject = contactObject;
        if (contactObject == null)
            return;

        Material mat = contactObject.GetComponent<MeshRenderer>().material;
        if (mat == null)
            return;
        mat.SetColor("_BaseColor", contactColor);
     }

    public void DeleteAtContactObject()
    {
        if (contactObject == null)
            return;

        int idx = _markers.IndexOf(contactObject);
        DeleteAt(idx);
    }

    public void AddPoint()
    {
        if (_lineMain == null) return;

        Vector3 mainCamPos = _depthRaycast.TryPlace(_rectPlaceholder, _centerCam.transform, true, _centerCam);
        //mainCamPos = useDebugObject ? playerTracking.transform.position : mainCamPos;
        mainCamPos = useDebugObject ? playerTracking.transform.position : _centerCam.transform.position;
        // mainCamPos.z += initialHeightOffset;
        mainCamPos.y += initialHeightOffset;

        Vector3 storedPos = _lineMain.useWorldSpace? mainCamPos : _lineMain.transform.InverseTransformPoint(mainCamPos);
        _points.Add(storedPos);
        _initialPoints.Add(storedPos);
        ApplyPointsToLineRenderer();

        GameObject marker = CreateOrInstantiateMarker(mainCamPos);
        _markers.Add(marker);
        _initialMarkers.Add(marker.transform.position);
    }

    private void ApplyPointsToLineRenderer()
    {
        _lineMain.positionCount = _points.Count;

        if (_points.Count == 0) return;

        // We already store positions in the correct space; just set them.
        _lineMain.SetPositions(_points.ToArray());
    }

    private GameObject CreateOrInstantiateMarker(Vector3 worldPos)
    {
        GameObject marker;

        if (spherePrefab)
        {
            marker = Instantiate(spherePrefab, worldPos, Quaternion.identity,
                                 markersParent ? markersParent : null);
        }
        else
        {
            return null;
        }

        return marker;
    }

    private void DeleteAt(int index)
    {
        if (index < 0 || index >= _points.Count) return;

        // Remove marker
        if (_markers[index])
        {
            Destroy(_markers[index]);
        }
        _markers.RemoveAt(index);
        _initialMarkers.RemoveAt(index);

        // Remove point and update line
        _points.RemoveAt(index);
        _initialPoints.RemoveAt(index);
        ApplyPointsToLineRenderer();
    }
}
