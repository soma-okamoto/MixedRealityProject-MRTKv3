using System;
using System.Collections;
using System.Reflection;
using Meta.XR.EnvironmentDepth;
using RosSharp.RosBridgeClient;
using sensor_msgs = RosSharp.RosBridgeClient.MessageTypes.Sensor;
using UnityEngine;

/// <summary>
/// Publishes sensor_msgs/CameraInfo for the Meta Environment Depth camera.
///
/// EnvironmentDepthManager keeps the depth camera FOV in its internal frame
/// descriptors. This component reads those values through reflection because
/// the current SDK does not expose them as a public API.
/// </summary>
public class DepthCameraInfoPublisher : UnityPublisher<sensor_msgs.CameraInfo>
{
    private const string DepthTextureProperty = "_EnvironmentDepthTexture";

    [Header("Environment Depth")]
    [SerializeField]
    private EnvironmentDepthManager environmentDepthManager;

    [Header("ROS CameraInfo")]
    [SerializeField]
    private string frameId = "quest_depth_left_optical";

    [SerializeField]
    private int outputWidth = 640;

    [SerializeField]
    private int outputHeight = 640;

    [SerializeField]
    private string distortionModel = "plumb_bob";

    [Header("Readiness")]
    [SerializeField]
    [Min(0f)]
    private float waitTimeoutSeconds = 20f;

    private Coroutine publishCoroutine;

    protected override void Start()
    {
        if (environmentDepthManager == null)
            environmentDepthManager = FindObjectOfType<EnvironmentDepthManager>();

        if (string.IsNullOrWhiteSpace(Topic))
            Topic = "/meta_depth_camera_info";

        base.Start();
    }

    /// <summary>
    /// Publishes the current depth-camera intrinsics when the first depth
    /// frame is available.
    /// </summary>
    public void PublishCameraInfo()
    {
        if (publishCoroutine != null)
        {
            Debug.LogWarning("[DepthCameraInfo] Publish is already waiting for depth readiness.");
            return;
        }

        publishCoroutine = StartCoroutine(PublishWhenReady());
    }

    private IEnumerator PublishWhenReady()
    {
        var startTime = Time.realtimeSinceStartup;

        while (environmentDepthManager == null ||
               !environmentDepthManager.IsDepthAvailable ||
               Shader.GetGlobalTexture(DepthTextureProperty) == null)
        {
            if (waitTimeoutSeconds > 0f &&
                Time.realtimeSinceStartup - startTime >= waitTimeoutSeconds)
            {
                Debug.LogError("[DepthCameraInfo] Environment Depth was not ready before timeout.");
                publishCoroutine = null;
                yield break;
            }

            yield return null;
        }

        PublishCurrentCameraInfo();
        publishCoroutine = null;
    }

    private void PublishCurrentCameraInfo()
    {
        if (!TryGetDepthFov(out var left, out var right, out var top, out var down))
        {
            Debug.LogError("[DepthCameraInfo] Could not read Environment Depth FOV from the SDK.");
            return;
        }

        var width = Mathf.Max(1, outputWidth);
        var height = Mathf.Max(1, outputHeight);
        var horizontalFov = left + right;
        var verticalFov = top + down;

        if (horizontalFov <= 0f || verticalFov <= 0f)
        {
            Debug.LogError("[DepthCameraInfo] Invalid Environment Depth FOV values.");
            return;
        }

        // These equations match the asymmetric projection matrix used by
        // Meta's Environment Depth API. cy is expressed in ROS's top-down
        // image coordinates, hence the use of the top tangent.
        var fx = width / horizontalFov;
        var fy = height / verticalFov;
        var cx = width * left / horizontalFov;
        var cy = height * top / verticalFov;

        var message = new sensor_msgs.CameraInfo
        {
            height = (uint)height,
            width = (uint)width,
            distortion_model = distortionModel,
            D = new double[5],
            K = new[]
            {
                (double)fx, 0.0, (double)cx,
                0.0, (double)fy, (double)cy,
                0.0, 0.0, 1.0
            },
            R = new[]
            {
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0
            },
            P = new[]
            {
                (double)fx, 0.0, (double)cx, 0.0,
                0.0, (double)fy, (double)cy, 0.0,
                0.0, 0.0, 1.0, 0.0
            },
            binning_x = 0,
            binning_y = 0
        };

        message.header.frame_id = frameId;
        message.header.Update();
        Publish(message);

        Debug.Log($"[DepthCameraInfo] Published: output={width}x{height}, " +
                  $"fx={fx:F3}, fy={fy:F3}, cx={cx:F3}, cy={cy:F3}, topic={Topic}");
    }

    private bool TryGetDepthFov(out float left, out float right, out float top, out float down)
    {
        left = 0f;
        right = 0f;
        top = 0f;
        down = 0f;

        if (environmentDepthManager == null)
            return false;

        try
        {
            var managerType = typeof(EnvironmentDepthManager);
            var descriptorsField = managerType.GetField(
                "frameDescriptors",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (descriptorsField == null)
                return false;

            var descriptors = descriptorsField.GetValue(environmentDepthManager) as Array;
            if (descriptors == null || descriptors.Length == 0)
                return false;

            var descriptor = descriptors.GetValue(0);
            if (descriptor == null)
                return false;

            var descriptorType = descriptor.GetType();
            left = ReadDescriptorFloat(descriptorType, descriptor, "fovLeftAngleTangent");
            right = ReadDescriptorFloat(descriptorType, descriptor, "fovRightAngleTangent");
            top = ReadDescriptorFloat(descriptorType, descriptor, "fovTopAngleTangent");
            down = ReadDescriptorFloat(descriptorType, descriptor, "fovDownAngleTangent");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[DepthCameraInfo] Failed to read SDK depth descriptor: {exception.Message}");
            return false;
        }
    }

    private static float ReadDescriptorFloat(Type descriptorType, object descriptor, string fieldName)
    {
        var field = descriptorType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new MissingFieldException(descriptorType.FullName, fieldName);

        return (float)field.GetValue(descriptor);
    }

    private void OnDisable()
    {
        if (publishCoroutine != null)
        {
            StopCoroutine(publishCoroutine);
            publishCoroutine = null;
        }
    }
}
