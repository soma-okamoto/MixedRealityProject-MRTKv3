using System;
using System.Collections;
using Meta.XR.EnvironmentDepth;
using PassthroughCameraSamples;
using RosSharp.RosBridgeClient;
using sensor_msgs = RosSharp.RosBridgeClient.MessageTypes.Sensor;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif


    /// <summary>
    /// Publishes a finite burst of environment-depth images on request.
    /// Depth values are published as 32-bit floating-point meters (32FC1).
    /// </summary>
    public class DepthImageTriggerPublisher : UnityPublisher<sensor_msgs.Image>
    {
        private const string DepthTextureProperty = "_EnvironmentDepthTexture";
        private const string DepthParamsProperty = "_EnvironmentDepthZBufferParams";

        [Header("Depth conversion")]
        [SerializeField]
        private Shader depthLinearizationShader;

        [SerializeField]
        private string frameId = "quest_depth_optical";

        [SerializeField]
        private int outputWidth = 640;

        [SerializeField]
        private int outputHeight = 480;

        [Header("Triggered capture")]
        [SerializeField]
        private int framesPerRequest = 3;

        [SerializeField]
        private float intervalSeconds = 0.1f;

        [Header("Environment Depth readiness")]
        [SerializeField]
        private EnvironmentDepthManager environmentDepthManager;

        [SerializeField]
        [Min(0f)]
        private float depthWaitTimeoutSeconds = 20f;

        private Material depthMaterial;
        private RenderTexture captureTarget;
        private Texture2D readbackTexture;
        private Coroutine captureCoroutine;

        public bool IsCapturing => captureCoroutine != null;

        private void Awake()
        {
            if (environmentDepthManager == null)
                environmentDepthManager = FindObjectOfType<EnvironmentDepthManager>();

            Debug.Log($"[DepthImage] Awake. enabled={enabled}, active={gameObject.activeInHierarchy}");
        }

        private void OnEnable()
        {
            Debug.Log("[DepthImage] OnEnable");
        }

        protected override void Start()
        {
            Debug.Log("[DepthImage] Start");

            if (string.IsNullOrWhiteSpace(Topic))
                Topic = "/camera/depth/image_raw";

            if (depthLinearizationShader == null)
            {
                depthLinearizationShader = Shader.Find("Hidden/Appletea/DepthImageToMeters");
            }

            if (depthLinearizationShader == null)
            {
                Debug.LogError("DepthImageToMeters shader was not found.");
                enabled = false;
                return;
            }

            Debug.Log($"[DepthImage] Shader ready: {depthLinearizationShader.name}, topic={Topic}");
            Debug.Log($"[DepthImage] EnvironmentDepthManager: found={environmentDepthManager != null}, " +
                      $"enabled={environmentDepthManager != null && environmentDepthManager.enabled}, " +
                      $"supported={EnvironmentDepthManager.IsSupported}");

            depthMaterial = new Material(depthLinearizationShader)
            {
                name = "DepthImageTriggerPublisher_LinearizationMaterial"
            };

            base.Start();
        }

        /// <summary>
        /// UnityEvent-compatible trigger. Captures framesPerRequest frames.
        /// </summary>
        public void CaptureAndPublish()
        {
            Debug.Log($"[DepthImage] CaptureAndPublish called. enabled={enabled}, active={isActiveAndEnabled}");
            RequestCapture(framesPerRequest);
        }

        public void RequestCapture(int frameCount)
        {
            if (captureCoroutine != null)
            {
                Debug.LogWarning("Depth capture is already running.");
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

            Debug.Log($"[DepthImage] CaptureBurst started. requestedFrames={frameCount}");

            var waitingForTextureLogged = false;
            var lastStatusLogTime = -Mathf.Infinity;
            var waitStartTime = Time.realtimeSinceStartup;

            while (Shader.GetGlobalTexture(DepthTextureProperty) == null)
            {
                if (!waitingForTextureLogged)
                {
                    Debug.Log("[DepthImage] Waiting for _EnvironmentDepthTexture... " +
                              "EnvironmentDepthManagerが最初のDepthフレームを受信するまで待機します。");
                    waitingForTextureLogged = true;
                }

                if (Time.realtimeSinceStartup - lastStatusLogTime >= 2f)
                {
                    lastStatusLogTime = Time.realtimeSinceStartup;
                    LogDepthReadiness();
                }

                if (depthWaitTimeoutSeconds > 0f &&
                    Time.realtimeSinceStartup - waitStartTime >= depthWaitTimeoutSeconds)
                {
                    Debug.LogError("[DepthImage] _EnvironmentDepthTextureが準備されないため、Depth撮影を中止しました。" +
                                    " Scene権限、EnvironmentDepthManager、QuestのDepth API対応状況を確認してください。");
                    captureCoroutine = null;
                    yield break;
                }

                yield return null;
            }

            Debug.Log("[DepthImage] _EnvironmentDepthTexture is ready.");

            var width = Mathf.Max(1, outputWidth);
            var height = Mathf.Max(1, outputHeight);
            EnsureCaptureResources(width, height);

            var publishedCount = 0;
            while (publishedCount < frameCount)
            {
                // EnvironmentDepthManager updates the global texture once per frame.
                yield return new WaitForEndOfFrame();

                if (TryPublishFrame())
                    publishedCount++;

                if (publishedCount < frameCount && intervalSeconds > 0f)
                    yield return new WaitForSeconds(intervalSeconds);
            }

            Debug.Log($"Depth capture finished. Published {publishedCount}/{frameCount} frame(s).");
            captureCoroutine = null;
        }

        private void LogDepthReadiness()
        {
            var globalDepthTexture = Shader.GetGlobalTexture(DepthTextureProperty);
            var managerState = environmentDepthManager == null
                ? "not found"
                : $"enabled={environmentDepthManager.enabled}, active={environmentDepthManager.isActiveAndEnabled}, " +
                  $"isDepthAvailable={environmentDepthManager.IsDepthAvailable}";

#if UNITY_ANDROID
            var scenePermission = Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission);
#else
            const string scenePermission = "n/a (not Android)";
#endif

            Debug.Log($"[DepthImage] Depth status: texture={(globalDepthTexture != null ? "ready" : "null")}, " +
                      $"manager={managerState}, supported={EnvironmentDepthManager.IsSupported}, " +
                      $"scenePermission={scenePermission}");
        }

        private bool TryPublishFrame()
        {
            var depthTexture = Shader.GetGlobalTexture(DepthTextureProperty);
            if (depthTexture == null || depthMaterial == null || captureTarget == null || readbackTexture == null)
                return false;

            var depthParams = Shader.GetGlobalVector(DepthParamsProperty);
            if (Mathf.Approximately(depthParams.x, 0f))
            {
                Debug.LogWarning("Environment depth parameters are not ready.");
                return false;
            }

            depthMaterial.SetTexture(DepthTextureProperty, depthTexture);
            depthMaterial.SetVector(DepthParamsProperty, depthParams);
            Graphics.Blit(Texture2D.blackTexture, captureTarget, depthMaterial);

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

            var pixels = readbackTexture.GetPixels();
            var values = new float[pixels.Length];

            // Convert Unity's bottom-to-top pixel order to ROS's top-to-bottom order.
            for (var y = 0; y < captureTarget.height; y++)
            {
                var sourceY = captureTarget.height - 1 - y;

                for (var x = 0; x < captureTarget.width; x++)
                {
                    var sourceIndex = sourceY * captureTarget.width + x;
                    var rosIndex = y * captureTarget.width + x;
                    values[rosIndex] = pixels[sourceIndex].r;
                }
            }

            var data = new byte[values.Length * sizeof(float)];
            Buffer.BlockCopy(values, 0, data, 0, data.Length);

            var message = new sensor_msgs.Image
            {
                height = (uint)captureTarget.height,
                width = (uint)captureTarget.width,
                encoding = "32FC1",
                is_bigendian = 0,
                step = (uint)(captureTarget.width * sizeof(float)),
                data = data
            };

            message.header.frame_id = frameId;
            message.header.Update();
            Publish(message);

            Debug.Log($"Published depth image: {message.width}x{message.height}, encoding=32FC1, unit=m, bytes={data.Length}, topic={Topic}");
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

            captureTarget = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat)
            {
                name = "DepthImageTriggerPublisher_CaptureTarget",
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = false
            };
            captureTarget.Create();

            readbackTexture = new Texture2D(width, height, TextureFormat.RFloat, false)
            {
                name = "DepthImageTriggerPublisher_Readback"
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

            if (depthMaterial != null)
            {
                Destroy(depthMaterial);
                depthMaterial = null;
            }
        }
    }
