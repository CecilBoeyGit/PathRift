// Copyright 2022-2024 Niantic.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Niantic.Lightship.AR.ObjectDetection;
using Niantic.Lightship.AR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.Lightship.MetaQuest.InternalSamples
{
    public class ObjectDetectionFiltered : MonoBehaviour
    {
        [Header("DEPTH DETECTION")]
/*        [SerializeField]
        private DepthDetection _depthDetectionResult;*/
        [SerializeField]
        private RaycastDepth _depthRaycast;

        // Pools: put your 2 ObjectPoolingUniversal components here in Inspector
        [SerializeField] private ObjectPoolingUniversal[] pools;

        // Category config
        [SerializeField] private List<string> StationaryCategories = new() { "Bench", "TrafficLight", "Sign" };

        // Distances (meters)
        [SerializeField] private float movingKeepDistance = 2.0f;      // moving markers MUST have a detection within this to stay
        [SerializeField] private float stationarySpawnDistance = 10.0f; // spawn a new stationary marker if all existing are farther

        // Tracking
        private readonly List<PooledMarker> _moving = new();
        private readonly List<PooledMarker> _stationary = new();

        // Temp buffers for current-frame detections
        private readonly List<Vector3> _vehicleDetections = new();
        private readonly List<Vector3> _stationaryDetections = new();


        /*        [SerializeField]
                private Text _DebugText;*/
        /*        [SerializeField]
                private GameObject _DebugCube;*/

        [SerializeField]
        private DrawRectCustom _drawRect;
        [SerializeField]
        private DrawRect _drawRectOriginal;

        [SerializeField]
        private Camera _centerCam;

        [SerializeField]
        private ARObjectDetectionManager _objectDetectionManager;

        [SerializeField]
        private float _probabilityThreshold = 0.5f;

        // Colors to assign to each category
        private readonly Color[] _colors =
        {
            Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black
        };

        private void Start()
        {
            if(_depthRaycast == null)
            {
                Debug.LogError("Please assign DepthDetection Script! ---");
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (!_objectDetectionManager)
            {
                Debug.LogError("Please assign ARObjectDetectionManager! ---");
                enabled = false;
                return;
            }

            _objectDetectionManager.MetadataInitialized += ObjectDetectionManager_OnMetadataInitialized;
        }

        private void OnDisable()
        {
            _objectDetectionManager.MetadataInitialized -= ObjectDetectionManager_OnMetadataInitialized;
            _objectDetectionManager.ObjectDetectionsUpdated -= ObjectDetectionManager_ObjectDetectionsUpdated;
        }

        private void ObjectDetectionManager_OnMetadataInitialized(ARObjectDetectionModelEventArgs args)
        {
            _objectDetectionManager.ObjectDetectionsUpdated += ObjectDetectionManager_ObjectDetectionsUpdated;
        }

        private void ObjectDetectionManager_ObjectDetectionsUpdated(ARObjectDetectionsUpdatedEventArgs args)
        {
            // UI cleanup only
            _drawRect.ClearRects();
            _drawRectOriginal.ClearRects();

            _vehicleDetections.Clear();
            _stationaryDetections.Clear();

            var result = args.Results;
            if (result == null || result.Count == 0)
            {
                return;
            }

            // Get the viewport resolution
            var viewport = _drawRect.GetComponent<RectTransform>();
            int viewportWidth = Mathf.FloorToInt(viewport.rect.width);
            int viewportHeight = Mathf.FloorToInt(viewport.rect.height);

            foreach (var detection in args.Results)
            {
                // Determine the classification category of this detection
                var categorizations = detection.GetConfidentCategorizations(_probabilityThreshold);
                if (categorizations == null || categorizations.Count == 0) continue;

                var best = categorizations.Aggregate((a, b) => a.Confidence > b.Confidence ? a : b);
                string categoryName = best.CategoryName;
                float confidence = detection.GetConfidence(categoryName);
                if (confidence < _probabilityThreshold) continue;

                // Get the bounding rect around the detected object
                var rect = detection.CalculateRect(viewportWidth, viewportHeight,
                    XRDisplayContext.GetScreenOrientation());

                Rect screenCenter = new Rect(new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f),
                                            rect.size);

                // Draw the bounding rect around the detected object
                var info = $"{categoryName}: {confidence}\n";
                _drawRect.CreateRect(screenCenter, GetOrAssignColorToCategory(categoryName), info, out RectTransform _outRectT);
                Vector3 worldPos = _depthRaycast.TryPlace(_outRectT, transform, false, _centerCam);

                bool isVehicle = string.Equals(categoryName, "Vehicle", StringComparison.OrdinalIgnoreCase);
                bool isStationaryCat = !string.Equals(categoryName, "Vehicle", StringComparison.OrdinalIgnoreCase);
                //bool isStationaryCat = StationaryCategories.Exists(s => string.Equals(s, categoryName, StringComparison.OrdinalIgnoreCase));

                if (isVehicle)
                {
                    _vehicleDetections.Add(worldPos);

                    // Attach/update nearest moving marker; if too far, spawn a new one
                    if (TryFindNearest(_moving, worldPos, out var nearest, out var sqr))
                    {
                        float keepSqr = movingKeepDistance * movingKeepDistance;
                        if (sqr <= keepSqr)
                        {
                            // close enough → update position (smooth optional)
                            nearest.transform.position = worldPos;
                        }
                        else
                        {
                            // too far from all moving → spawn a fresh moving marker
                            SpawnMarker(worldPos, isMoving: true, isStationary: false);
                        }
                    }
                    else
                    {
                        // none exist yet → spawn first
                        SpawnMarker(worldPos, isMoving: true, isStationary: false);
                    }
                }
                else if (isStationaryCat)
                {
                    _stationaryDetections.Add(worldPos);

                    // If new detection is far from ALL existing stationary markers, spawn a new one.
                    if (!TryFindNearest(_stationary, worldPos, out var nearestS, out var sqrS) ||
                        sqrS > (stationarySpawnDistance * stationarySpawnDistance))
                    {
                        SpawnMarker(worldPos, isMoving: false, isStationary: true);
                    }
                    // else: close to an existing stationary marker → do nothing (we keep the old one)
                }
                // else: ignore other categories
            }

            // --- RECONCILE / CLEANUP ---

            // For MOVING: any marker that is not near at least one vehicle detection this frame is returned to pool.
            if (_moving.Count > 0)
            {
                float keepSqr = movingKeepDistance * movingKeepDistance;
                for (int i = _moving.Count - 1; i >= 0; i--)
                {
                    var m = _moving[i];
                    bool hasNearbyDetection = false;
                    for (int d = 0; d < _vehicleDetections.Count; d++)
                    {
                        if (SqrDist(m.transform.position, _vehicleDetections[d]) <= keepSqr)
                        {
                            hasNearbyDetection = true;
                            break;
                        }
                    }

                    if (!hasNearbyDetection)
                    {
                        m.Return();
                        _moving.RemoveAt(i);
                    }
                }
            }
        }

        private void ProbabilityThresholdSlider_OnThresholdChanged(float newThreshold)
        {
            _probabilityThreshold = newThreshold;
        }

        private Color GetOrAssignColorToCategory(string categoryName)
        {
            // Return a constant color
            Color col = Color.cyan;
            col.a = 0.2f;
            return col;
        }


        //----------------------------------------------------------------------

        // Object Pooling helper functions

        //----------------------------------------------------------------------
        private ObjectPoolingUniversal RandomPool()
        {
            if (pools == null || pools.Length == 0) return null;
            int i = UnityEngine.Random.Range(0, pools.Length);
            return pools[i];
        }

        private static float SqrDist(Vector3 a, Vector3 b) => (a - b).sqrMagnitude;

        private static bool TryFindNearest(IList<PooledMarker> list, Vector3 p, out PooledMarker nearest, out float sqrDist)
        {
            nearest = null;
            sqrDist = float.PositiveInfinity;
            for (int i = 0; i < list.Count; i++)
            {
                float d = SqrDist(list[i].transform.position, p);
                if (d < sqrDist) { sqrDist = d; nearest = list[i]; }
            }
            return nearest != null;
        }

        private PooledMarker SpawnMarker(Vector3 pos, bool isMoving, bool isStationary)
        {
            var pool = RandomPool();
            if (pool == null) return null;

            var tr = pool.SpawnAt(pos);
            if (tr == null) return null;

            var tag = tr.GetComponent<PooledMarker>();
            if (tag == null) tag = tr.gameObject.AddComponent<PooledMarker>();
            tag.moving = isMoving;
            tag.stationary = isStationary;
            tag.ownerPool = pool;

            if (isMoving) _moving.Add(tag);
            if (isStationary) _stationary.Add(tag);

            return tag;
        }

    }
}
