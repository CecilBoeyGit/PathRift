// Copyright 2022-2024 Niantic.

using System;
using System.Collections.Generic;
using System.Collections;
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
        [SerializeField] private float movingKeepDistance = 3.0f;      // moving markers MUST have a detection within this to stay
        [SerializeField] private float stationarySpawnDistance = 20.0f; // spawn a new stationary marker if all existing are farther

        // Tracking
        public List<PooledMarker> _moving = new();
        public List<PooledMarker> _stationary = new();

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

        [SerializeField]
        private float _bboxSizeThreshold = 6.0f;

        [SerializeField]
        private float _minDistanceThreshold = 8.0f;
        [SerializeField]
        private float _maxDistanceThreshold = 15.0f;

        [SerializeField]
        private float _detectionInterval = 2.0f;
        private float timer;
        bool canDetect = false;

        bool maxEnemiesCountReached = false;



[Header("Health Cross Settings")]
[SerializeField] private ObjectPoolingUniversal healthCrossPool;

// Tracks active crosses per detection
private Dictionary<int, PooledMarker> activeHealthCrosses = new();
private HashSet<int> personsDetectedThisFrame = new();






        bool _StartGame = false;

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

        private bool isDelaying = false;

        private void Update()
        {
            timer += Time.deltaTime;

            // If we hit the detection interval and we're not already delaying
            if (timer >= _detectionInterval && !isDelaying)
            {
                StartCoroutine(DelayBeforeReset());
            }

            if(_moving.Count + _stationary.Count >= 8)
            {
                maxEnemiesCountReached = true;
            }
            else
            {
                maxEnemiesCountReached = false;
            }
        }

        public void StartGame()
        {
             StartCoroutine(StartGameProcess());
        }

        private IEnumerator StartGameProcess()
        {
            _StartGame = false;

            yield return new WaitForSeconds(7.0f);

            _StartGame = true;
        }

        private IEnumerator DelayBeforeReset()
        {
            isDelaying = true;
            canDetect = true;

            yield return new WaitForSeconds(1.0f); 

            canDetect = false;
            timer = 0f;
            isDelaying = false;
        }

        private void ObjectDetectionManager_OnMetadataInitialized(ARObjectDetectionModelEventArgs args)
        {
            _objectDetectionManager.ObjectDetectionsUpdated += ObjectDetectionManager_ObjectDetectionsUpdated;
        }

Vector3 refTrackingVector;

        private void ObjectDetectionManager_ObjectDetectionsUpdated(ARObjectDetectionsUpdatedEventArgs args)
        {
            if(!_StartGame)
                return;

            // UI cleanup only
            _drawRect.ClearRects();
            _drawRectOriginal.ClearRects();

            _vehicleDetections.Clear();
            _stationaryDetections.Clear();

            personsDetectedThisFrame.Clear();


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
                // ------------------------------------------------------------------------------------------------------------------------------------------------

                // Original world position output from Raycasting against depth

                Vector3 worldPos = _depthRaycast.TryPlace(_outRectT, transform, false, _centerCam);
                if(worldPos == Vector3.zero)
                    continue;

                float distanceToPlayer = Vector3.Distance(worldPos, _centerCam.transform.position);

                // ------------------------------------------------------------------------------------------------------------------------------------------------

                bool isVehicle = string.Equals(categoryName, "Vehicle", StringComparison.OrdinalIgnoreCase);
                bool isPerson = string.Equals(categoryName, "Person", StringComparison.OrdinalIgnoreCase);
                bool isStationaryCat = !string.Equals(categoryName, "Vehicle", StringComparison.OrdinalIgnoreCase);
                //bool isStationaryCat = StationaryCategories.Exists(s => string.Equals(s, categoryName, StringComparison.OrdinalIgnoreCase));

                //Vector3 worldPos;
                //Vector2 hitWorldSize;
                //bool hasDepth = _depthRaycast.TryGetSize(_outRectT, transform, false, _centerCam, out worldPos, out hitWorldSize);



                // -------------------------------
                // PERSON DETECTION HANDLING
                // -------------------------------
                if (isPerson)
                {
                    // Create a stable ID for this detection using rect hash
                    int detectionID = rect.GetHashCode();

                    personsDetectedThisFrame.Add(detectionID);

                    // Already exists → update cross position
                    if (activeHealthCrosses.TryGetValue(detectionID, out var existingCross))
                    {
                        existingCross.transform.position = worldPos;
                    }
                    else
                    {
                        // New Person → spawn a new HealthCross
                        var tr = healthCrossPool.SpawnAt(worldPos);
                        if (tr != null)
                        {
                            var marker = tr.GetComponent<PooledMarker>();
                            if (marker == null)
                                marker = tr.gameObject.AddComponent<PooledMarker>();

                            marker.ownerPool = healthCrossPool;

                            activeHealthCrosses.Add(detectionID, marker);
                        }
                    }
                }




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
                            if (canDetect && !maxEnemiesCountReached)
                            {
                                if(distanceToPlayer >= _minDistanceThreshold && distanceToPlayer <= _maxDistanceThreshold)
                                {
                                    // too far from all moving → spawn a fresh moving marker
                                    var currentTag = SpawnMarker(worldPos, isMoving: true, isStationary: false);

                                    /*                            if (hitWorldSize.x <= _bboxSizeThreshold)
                                                                    return;*/
                                }
                            }
                        }
                    }
                    else
                    {
                        if (canDetect && !maxEnemiesCountReached)
                        {

                                if(distanceToPlayer >= _minDistanceThreshold && distanceToPlayer <= _maxDistanceThreshold)
                                {
                                    // too far from all moving → spawn a fresh moving marker
                                    var currentTag = SpawnMarker(worldPos, isMoving: true, isStationary: false);

                                    /*                            if (hitWorldSize.x <= _bboxSizeThreshold)
                                                                    return;*/
                                }
                        }
                    }
                }
                else if (isStationaryCat)
                {
                    _stationaryDetections.Add(worldPos);

                    // If new detection is far from ALL existing stationary markers, spawn a new one.
                    if (!TryFindNearest(_stationary, worldPos, out var nearestS, out var sqrS) ||
                        sqrS > (stationarySpawnDistance * stationarySpawnDistance))
                    {
                        if (canDetect && !maxEnemiesCountReached)
                        {
                                if(distanceToPlayer > _minDistanceThreshold && distanceToPlayer < _maxDistanceThreshold)
                                {
                            var currentTag = SpawnMarker(worldPos, isMoving: false, isStationary: true);

                            float distDoubleCheck = Vector3.Distance(currentTag.gameObject.transform.position, _centerCam.transform.position);
                            if(distDoubleCheck < _minDistanceThreshold)
                                {
                                    currentTag.Return();
                                    int index = _stationary.IndexOf(currentTag);
                                    _stationary.RemoveAt(index);
                                }

                            /*                        if (hitWorldSize.x <= _bboxSizeThreshold)
                                                        return;*/

                            // float radius = 1.5f;
                            // // Spawn two randomized markers around the input position
                            // for (int i = 0; i < 2; i++)
                            // {
                            //     Vector2 rand = UnityEngine.Random.insideUnitCircle * radius;
                            //     Vector3 offsetPos = worldPos + new Vector3(rand.x, 0f, rand.y);

                            //     SpawnMarker(worldPos, isMoving: false, isStationary: true);
                            // }
                                }
                        }
                    }
                    // else: close to an existing stationary marker → do nothing (we keep the old one)
                }
                // else: ignore other categories
            }
            // -------------------------------



            // FORLOOP END
            


            // -------------------------------




            // -------------------------------
            // CLEANUP LOST PERSON DETECTIONS
            // -------------------------------

            List<int> toRemove = new List<int>();

            foreach (var kvp in activeHealthCrosses)
            {
                int id = kvp.Key;
                if (!personsDetectedThisFrame.Contains(id))
                {
                    kvp.Value.Return();
                    toRemove.Add(id);
                }
            }

            // Remove cleaned entries
            foreach (int id in toRemove)
                activeHealthCrosses.Remove(id);

            // -------------------------------
            // CLEANUP LOST PERSON DETECTIONS
            // -------------------------------





            // --- RECONCILE / CLEANUP ---

            // For MOVING: any marker that is not near at least one vehicle detection this frame is returned to pool.
            if (_moving.Count > 0)
            {
                float keepSqr = movingKeepDistance * movingKeepDistance;
                for (int i = _moving.Count - 1; i >= 0; i--)
                {
                    int nearestIndex = -1;
                    var m = _moving[i];
                    var enemy = m.GetComponent<Enemy3>();

                    bool hasNearbyDetection = false;
                    for (int d = 0; d < _vehicleDetections.Count; d++)
                    {
                        if (SqrDist(m.transform.position, _vehicleDetections[d]) <= keepSqr)
                        {
                            hasNearbyDetection = true;
                            nearestIndex = d;
                            break;
                        }
                    }

                    if (!hasNearbyDetection)
                    {
                        if(enemy != null)
                        {
                            if (!enemy.unfollow)
                            {
                                enemy.unfollow = true;
                            }
                        }

                        //m.Return();
                        //_moving.RemoveAt(i);
                    }
                    else
                    {
                        if(enemy != null)
                        {
                            if(enemy.trackTimer > 0)
                            {
                                Vector3 nearestTrackingPos = _vehicleDetections[nearestIndex];
                                m.transform.position = Vector3.SmoothDamp(m.transform.position, nearestTrackingPos, ref refTrackingVector, 10.0f);
                                enemy.unfollow = false;
                                enemy.trackTimer = 1;
                            }
                        }
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
            col.a = 0.0f;
            return col;
        }


        //----------------------------------------------------------------------

        // Object Pooling helper functions

        //----------------------------------------------------------------------
        private ObjectPoolingUniversal RandomPool(bool isMoving)
        {
            if (isMoving)
            {
                if (pools == null || pools.Length == 0) return null;
                return pools[0];
            }
            else
            {
                if (pools == null || pools.Length == 0) return null;
                int i = UnityEngine.Random.Range(1, pools.Length);
                return pools[i];
            }
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
            var pool = RandomPool(isMoving);
            if (pool == null) return null;

            var tr = pool.SpawnAt(pos);
            if (tr == null) return null;

            var tag = tr.GetComponent<PooledMarker>();
            if (tag == null) tag = tr.gameObject.AddComponent<PooledMarker>();
            tag.DetectionResultsManager = this;
            tag.moving = isMoving;
            tag.stationary = isStationary;
            tag.ownerPool = pool;

            if (isMoving) _moving.Add(tag);
            if (isStationary) _stationary.Add(tag);

            return tag;
        }

    }
}
