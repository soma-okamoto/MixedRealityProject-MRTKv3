using System.Collections;
using PassthroughCameraSamples;
using RosSharp.RosBridgeClient;
using sensor_msgs = RosSharp.RosBridgeClient.MessageTypes.Sensor;
using UnityEngine;

using RosSharp.RosBridgeClient.MessageTypes.Std;


    /// <summary>
    /// Publishes a small burst of Passthrough Camera frames only when requested.
    ///
    /// Attach this component to the same GameObject as RosConnector and assign
    /// the WebCamTextureManager used by the CameraViewer sample.
    /// </summary>
    public class ColorImageTriggerPublisher : UnityPublisher<sensor_msgs.Image>
    {
        [Header("Passthrough Camera")]
        [SerializeField]
        private WebCamTextureManager webCamTextureManager;

        [Header("ROS image")]
        [SerializeField]
        private string frameId = "quest_color_optical";

        [SerializeField]
        private int outputWidth = 640;

        [SerializeField]
        private int outputHeight = 640;

        [Header("Triggered capture")]
        [SerializeField]
        private int framesPerRequest = 3;

        [SerializeField]
        private float intervalSeconds = 0.1f;

        [Tooltip("Unity Texture2D pixels are bottom-to-top, while ROS images are top-to-bottom.")]
        [SerializeField]
        private bool flipVertical = true;

        [Header("QR-relative camera pose")]
        [SerializeField]
        private MetaColorPoseQrPublisher metaColorPoseQrPublisher;

        private RenderTexture captureTarget;
        private Texture2D readbackTexture;
        private Coroutine captureCoroutine;

        public bool IsCapturing => captureCoroutine != null;

        protected override void Start()
        {
            if (webCamTextureManager == null)
                webCamTextureManager = FindFirstObjectByType<WebCamTextureManager>();

            if (metaColorPoseQrPublisher == null)
                metaColorPoseQrPublisher = FindObjectOfType<MetaColorPoseQrPublisher>();

            if (string.IsNullOrWhiteSpace(Topic))
                Topic = "/camera/color/image_raw";

            base.Start();
        }

        /// <summary>
        /// UnityEvent-compatible trigger. Captures framesPerRequest frames.
        /// </summary>
        public void CaptureAndPublish()
        {
            RequestCapture(framesPerRequest);
        }

        /// <summary>
        /// Starts a finite capture burst. A new request is ignored while a burst
        /// is already running, so one UI event cannot create overlapping bursts.
        /// </summary>
        public void RequestCapture(int frameCount)
        {
            if (captureCoroutine != null)
            {
                Debug.LogWarning("Color capture is already running.");
                return;
            }

            captureCoroutine = StartCoroutine(CaptureBurst(frameCount));
        }

        public void CancelCapture()
        {
            if (captureCoroutine == null)
                return;

            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }

        private IEnumerator CaptureBurst(int frameCount)
        {
            frameCount = Mathf.Max(1, frameCount);

            while (webCamTextureManager == null || webCamTextureManager.WebCamTexture == null)
                yield return null;

            var source = webCamTextureManager.WebCamTexture;
            while (!source.isPlaying || source.width <= 16 || source.height <= 16)
                yield return null;

            var width = outputWidth > 0 ? outputWidth : source.width;
            var height = outputHeight > 0 ? outputHeight : source.height;
            EnsureCaptureResources(width, height);

            var publishedCount = 0;
            while (publishedCount < frameCount)
            {
                yield return new WaitUntil(() => source != null && source.didUpdateThisFrame);
                yield return new WaitForEndOfFrame();

                if (source == null || !source.isPlaying)
                    break;

                if (TryPublishFrame(source))
                    publishedCount++;

                if (publishedCount < frameCount && intervalSeconds > 0f)
                    yield return new WaitForSeconds(intervalSeconds);
            }

            Debug.Log($"Color capture finished. Published {publishedCount}/{frameCount} frame(s).");
            captureCoroutine = null;
        }

        private bool TryPublishFrame(WebCamTexture source)
        {
            if (captureTarget == null || readbackTexture == null)
                return false;

            Graphics.Blit(source, captureTarget);

            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = captureTarget;
                readbackTexture.ReadPixels(
                    new Rect(0, 0, captureTarget.width, captureTarget.height),
                    0,
                    0,
                    false);
                readbackTexture.Apply(false, false);
            }
            finally
            {
                RenderTexture.active = previousActive;
            }

            var pixels = readbackTexture.GetPixels32();
            var data = new byte[pixels.Length * 3];

            for (var y = 0; y < captureTarget.height; y++)
            {
                var sourceY = flipVertical ? captureTarget.height - 1 - y : y;

                for (var x = 0; x < captureTarget.width; x++)
                {
                    var sourceIndex = sourceY * captureTarget.width + x;
                    var rosIndex = (y * captureTarget.width + x) * 3;
                    var pixel = pixels[sourceIndex];

                    // ROS encoding is rgb8. CvBridge can convert this to bgr8.
                    data[rosIndex + 0] = pixel.r;
                    data[rosIndex + 1] = pixel.g;
                    data[rosIndex + 2] = pixel.b;
                }
            }

            var message = new sensor_msgs.Image
            {
                height = (uint)captureTarget.height,
                width = (uint)captureTarget.width,
                encoding = "rgb8",
                is_bigendian = 0,
                step = (uint)(captureTarget.width * 3),
                data = data
            };

            message.header.frame_id = frameId;
            message.header.Update();

            // Publish the QR-relative camera pose with the exact same ROS
            // timestamp as this Color image. This is required when the head
            // moves during a multi-frame capture burst.
            if (metaColorPoseQrPublisher != null &&
                metaColorPoseQrPublisher.isActiveAndEnabled)
            {
                metaColorPoseQrPublisher.PublishPose(message.header.stamp);
            }

            Publish(message);

            Debug.Log($"Published color image: {message.width}x{message.height}, bytes={data.Length}, topic={Topic}");
            return true;
        }

        private void EnsureCaptureResources(int width, int height)
        {
            if (captureTarget != null &&
                captureTarget.width == width &&
                captureTarget.height == height &&
                readbackTexture != null)
            {
                return;
            }

            ReleaseCaptureResources();

            captureTarget = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "ColorImageTriggerPublisher_CaptureTarget",
                useMipMap = false,
                autoGenerateMips = false
            };
            captureTarget.Create();

            readbackTexture = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                name = "ColorImageTriggerPublisher_Readback"
            };
        }

        private void ReleaseCaptureResources()
        {
            if (captureTarget != null)
            {
                captureTarget.Release();
                Destroy(captureTarget);
                captureTarget = null;
            }

            if (readbackTexture != null)
            {
                Destroy(readbackTexture);
                readbackTexture = null;
            }
        }

        private void OnDisable()
        {
            CancelCapture();
        }

        private void OnDestroy()
        {
            ReleaseCaptureResources();
        }
    }
