using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Niantic.Lightship.AR.ObjectDetection;
using Niantic.Lightship.AR.Utilities;

public class ObjectDetectionResults : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ARObjectDetectionManager detectionManager;
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private RectTransform dotPrefab;      // UI Image/RectTransform (a small dot)

    [Header("Filter")]
    [Range(0f, 1f)] public float minConfidence = 0.6f;  // Detection confidence level
    [Tooltip("Accepted category names (case-insensitive)")]
    [SerializeField]
    private string[] vehicleNames =
        { "vehicle", "car", "automobile", "truck", "bus", "van", "taxi", "suv", "pickup" };

    [Header("Pool")]
    [SerializeField] private int maxDots = 16;

    private readonly List<RectTransform> _pool = new();
    private readonly List<RectTransform> _active = new();

    void Awake()
    {
        _canvasRect.GetComponent<RectTransform>();
        if (_canvasRect == null)
            print("NO CANVAS RECT!!!---");
    }

    void OnEnable()
    {
        if (!detectionManager)
        {
            Debug.LogError("[VehicleBBoxDotOverlay] Please assign ARObjectDetectionManager.");
            enabled = false;
            return;
        }

        // Warm up a small pool
        EnsurePool();

        detectionManager.enabled = true;
        detectionManager.MetadataInitialized += OnModelReady;
    }

    void OnDisable()
    {
        detectionManager.MetadataInitialized -= OnModelReady;
        detectionManager.ObjectDetectionsUpdated -= OnDetectionsUpdated;
        DeactivateAllDots();
    }

    private void OnModelReady(ARObjectDetectionModelEventArgs _)
    {
        detectionManager.ObjectDetectionsUpdated += OnDetectionsUpdated;
    }

    private void OnDetectionsUpdated(ARObjectDetectionsUpdatedEventArgs args)
    {
        // Hide all dots; we’ll reactivate only those needed this frame
        DeactivateAllDots();

        if (args.Results == null || args.Results.Count == 0)
            return;

        int sw = Mathf.FloorToInt(_canvasRect.rect.width);
        int sh = Mathf.FloorToInt(_canvasRect.rect.height);
        var orientation = XRDisplayContext.GetScreenOrientation();

        foreach (var det in args.Results)
        {
            // Filter by class + confidence
            var cats = det.GetConfidentCategorizations(minConfidence);
            if (cats == null || cats.Count == 0) continue;

/*            bool isVehicle = cats.Any(c =>
                vehicleNames.Any(v => string.Equals(c.CategoryName, v, StringComparison.OrdinalIgnoreCase)));

            if (!isVehicle) continue;*/

            Rect r = det.CalculateRect(sw, sh, orientation);
            Vector2 screenCenter = new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);

            var dot = GetDot();
            dot.anchoredPosition = new Vector2(r.x, r.y);
            dot.gameObject.SetActive(true);
            _active.Add(dot);

/*            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screenCenter, null, out Vector2 localPoint))
            {
                var dot = GetDot();
                dot.anchoredPosition = localPoint;
                dot.gameObject.SetActive(true);
                _active.Add(dot);
            }*/
        }
    }

    // --- pooling helpers ---

    private void EnsurePool()
    {
        if (!dotPrefab)
        {
            Debug.LogError("[VehicleBBoxDotOverlay] Please assign a dot prefab (RectTransform).");
            enabled = false;
            return;
        }

        // Create up to maxDots under the canvas
        for (int i = _pool.Count; i < maxDots; i++)
        {
            var inst = Instantiate(dotPrefab, _canvasRect);
            inst.gameObject.SetActive(false);
            _pool.Add(inst);
        }
    }

    private RectTransform GetDot()
    {
        // Reuse an inactive dot or expand the pool (clamped by maxDots)
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].gameObject.activeSelf)
                return _pool[i];
        }

        if (_pool.Count < maxDots)
        {
            var inst = Instantiate(dotPrefab, _canvasRect);
            inst.gameObject.SetActive(false);
            _pool.Add(inst);
            return inst;
        }

        // If we’re out, reuse the first active (simple fallback)
        return _active.Count > 0 ? _active[0] : _pool[0];
    }

    private void DeactivateAllDots()
    {
        for (int i = 0; i < _active.Count; i++)
            if (_active[i]) _active[i].gameObject.SetActive(false);
        _active.Clear();
    }
}
