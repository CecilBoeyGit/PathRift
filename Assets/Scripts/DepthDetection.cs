// Copyright 2022-2025 Niantic.

using Niantic.Lightship.AR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Niantic.Lightship.MetaQuest.InternalSamples
{
    public class DepthDetection : MonoBehaviour
    {
        private static readonly int s_displayMatrix = Shader.PropertyToID("_DisplayMatrix");

        [SerializeField]
        private AROcclusionManager _occlusionManager;

        [SerializeField]
        private DrawRectCustom _drawRect;

        [SerializeField]
        private Text _DebugText;

        private XRCpuImage? _depthImage;
        //private Texture _depthImage;
        private Matrix4x4 displayMatrix;

        private void Start()
        {

        }

        private void OnEnable()
        {
            _occlusionManager.frameReceived += OnFrameReceived;
        }

        private void OnDisable()
        {
            _occlusionManager.frameReceived -= OnFrameReceived;
        }

        private void OnFrameReceived(AROcclusionFrameEventArgs args)
        {
            if (args.externalTextures.Count <= 0)
            {
                return;
            }

            // Get a reference to the depth texture
            //_depthImage = args.externalTextures[0].texture;
            if (_occlusionManager.TryAcquireEnvironmentDepthCpuImage(out var image))
            {
                _depthImage?.Dispose();
                _depthImage = image;
            }

            // Get the width and height of the viewport
            var rectTransform = _drawRect.GetComponent<RectTransform>();
            var viewportWidth = (int)rectTransform.rect.width;
            var viewportHeight = (int)rectTransform.rect.height;
            var viewportOrientation = XRDisplayContext.GetScreenOrientation();

            displayMatrix = CameraMath.CalculateDisplayMatrix(
                _depthImage.Value.width,
                _depthImage.Value.height,
                viewportWidth,
                viewportHeight,
                viewportOrientation
                );

            // Calculate the display matrix for rendering the depth texture
            // on the RawImage UI element. The texture on Meta Quest usually
            // has a square resolution, so we use AffineMath directly here,
            // instead of CameraMath.CalculateDisplayMatrix to be able to
            // specify the image orientation.
            /*            var displayMatrix = AffineMath.Fit(
                            texture.width,
                            texture.height,
                            ScreenOrientation.LandscapeLeft,
                            viewportWidth,
                            viewportHeight,
                            viewportOrientation).transpose;*/
        }

        public Vector3 GetWorldPosition(Camera cam, Rect rect)
        {
            Vector2 uv = new Vector2(rect.x / rect.width, rect.y / rect.height);
            if (_depthImage.Value.Sample<float>(uv, displayMatrix) != 0)
            {
                var eyeDepth = _depthImage.Value.Sample<float>(uv, displayMatrix);
                _DebugText.text = eyeDepth.ToString();
                var worldPos = cam.ScreenToWorldPoint(new Vector3(rect.x, rect.y, eyeDepth));
                return worldPos;
            }

            _DebugText.text = "No Depth Image Value Sampled! ---";
            return Vector3.zero;

        }
    }
}
