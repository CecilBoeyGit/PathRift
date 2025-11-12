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
        [SerializeField]
        private ObjectPoolingUniversal _enemiesObjectPool;
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
            // Clear the previous bounding boxes
            _drawRect.ClearRects();
            _drawRectOriginal.ClearRects();
            _enemiesObjectPool.ClearRects();

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
                string categoryName;
                var categorizations = detection.GetConfidentCategorizations(_probabilityThreshold);
                if (categorizations.Count <= 0)
                {
                    continue;
                }

                // Sort the categorizations by confidence and select the most confident one
                categoryName = categorizations.Aggregate((a, b) => a.Confidence > b.Confidence ? a : b)
                    .CategoryName;

                // Filter out the objects with confidence less than the threshold
                float confidence = detection.GetConfidence(categoryName);
                if (confidence < _probabilityThreshold)
                {
                    continue;
                }

                // Get the bounding rect around the detected object
                var rect = detection.CalculateRect(viewportWidth, viewportHeight,
                    XRDisplayContext.GetScreenOrientation());

                Rect screenCenter = new Rect(new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f),
                                            rect.size);

                // Draw the bounding rect around the detected object
                var info = $"{categoryName}: {confidence}\n";
                _drawRect.CreateRect(screenCenter, GetOrAssignColorToCategory(categoryName), info, out RectTransform _outRectT);
                _depthRaycast.TryPlace(_outRectT, _centerCam);

                //_drawRectOriginal.CreateRect(rect, GetOrAssignColorToCategory(categoryName), info);

                /*                var _detWorldPos =_depthDetectionResult.GetWorldPosition(_centerCam, rect);
                                _DebugCube.transform.position = _detWorldPos;*/
                //_DebugText.text = _DebugCube.transform.position.ToString();
                //_enemiesObjectPool.CreateRect(_detWorldPos);
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
    }
}
