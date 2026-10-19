using System;
using System.Reflection;
using Meta.XR.EnvironmentDepth;
using PassthroughCameraSamples;
using RosSharp.RosBridgeClient;
using geometry_msgs = RosSharp.RosBridgeClient.MessageTypes.Geometry;
using tf2_msgs = RosSharp.RosBridgeClient.MessageTypes.Tf2;
using UnityEngine;

/// <summary>
/// Publishes the current extrinsic transform from the left color camera to
/// the left Environment Depth camera.
///
/// The transform is published as:
///   parent: quest_color_left_optical
///   child : quest_depth_left_optical
///
/// The Environment Depth SDK keeps the depth camera pose and tracking-space
/// matrix internal, so this component reads those values through reflection.
/// The reflection is isolated here; the point-cloud code is not modified.
/// </summary>
public class ColorDepthTransformPublisher : UnityPublisher<tf2_msgs.TFMessage>
{
    private const int LeftDepthDescriptorIndex = 0;

    [Header("Passthrough Camera")]
    [SerializeField]
    private PassthroughCameraEye colorCameraEye = PassthroughCameraEye.Left;

    [Header("Environment Depth")]
    [SerializeField]
    private EnvironmentDepthManager environmentDepthManager;

    [Header("TF frames")]
    [SerializeField]
    private string parentFrameId = "quest_color_left_optical";

    [SerializeField]
    private string childFrameId = "quest_depth_left_optical";

    private FieldInfo descriptorsField;
    private FieldInfo depthPositionField;
    private FieldInfo depthRotationField;
    private MethodInfo trackingSpaceWorldToLocalMethod;
    private bool reflectionInitialized;

    protected override void Start()
    {
        if (environmentDepthManager == null)
            environmentDepthManager = FindObjectOfType<EnvironmentDepthManager>();

        if (string.IsNullOrWhiteSpace(Topic))
            Topic = "/tf";

        base.Start();
    }

    /// <summary>
    /// Publishes one current Color-to-Depth transform. Call this at the same
    /// trigger as the image and CameraInfo messages.
    /// </summary>
    public void PublishTransform()
    {
        if (environmentDepthManager == null)
        {
            Debug.LogError("[ColorDepthTF] EnvironmentDepthManager was not found.");
            return;
        }

        if (!environmentDepthManager.IsDepthAvailable)
        {
            Debug.LogWarning("[ColorDepthTF] Environment Depth is not available yet.");
            return;
        }

        try
        {
            var colorPose = PassthroughCameraUtils.GetCameraPoseInWorld(colorCameraEye);

            if (!TryGetDepthPoseInWorld(out var depthPose))
            {
                Debug.LogError("[ColorDepthTF] Could not read the left Environment Depth pose.");
                return;
            }

            // Unity camera coordinates use x-right, y-up, z-forward. ROS
            // optical coordinates use x-right, y-down, z-forward.
            var worldFromColorUnity = Matrix4x4.TRS(
                colorPose.position, colorPose.rotation, Vector3.one);
            var worldFromDepthUnity = Matrix4x4.TRS(
                depthPose.position, depthPose.rotation, Vector3.one);
            var colorFromDepthUnity =
                worldFromColorUnity.inverse * worldFromDepthUnity;

            var unityToRosOptical = Matrix4x4.Scale(new Vector3(1f, -1f, 1f));
            var colorRosFromDepthRos =
                unityToRosOptical * colorFromDepthUnity * unityToRosOptical;

            var translation = colorRosFromDepthRos.GetColumn(3);
            var rotation = Quaternion.Normalize(colorRosFromDepthRos.rotation);

            var transform = new geometry_msgs.Transform(
                new geometry_msgs.Vector3(
                    translation.x, translation.y, translation.z),
                new geometry_msgs.Quaternion(
                    rotation.x, rotation.y, rotation.z, rotation.w));

            var stampedTransform = new geometry_msgs.TransformStamped
            {
                child_frame_id = childFrameId,
                transform = transform
            };
            stampedTransform.header.frame_id = parentFrameId;
            stampedTransform.header.Update();

            Publish(new tf2_msgs.TFMessage(new[] { stampedTransform }));

            Debug.Log($"[ColorDepthTF] Published {parentFrameId} -> {childFrameId}: " +
                      $"translation=({translation.x:F4}, {translation.y:F4}, {translation.z:F4}), " +
                      $"rotation=({rotation.x:F4}, {rotation.y:F4}, " +
                      $"{rotation.z:F4}, {rotation.w:F4}), topic={Topic}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ColorDepthTF] Failed to publish transform: {exception.Message}");
        }
    }

    private bool TryGetDepthPoseInWorld(out Pose depthPose)
    {
        depthPose = default;

        if (!TryInitializeReflection())
            return false;

        try
        {
            var descriptors = descriptorsField.GetValue(environmentDepthManager) as Array;
            if (descriptors == null || descriptors.Length <= LeftDepthDescriptorIndex)
                return false;

            var descriptor = descriptors.GetValue(LeftDepthDescriptorIndex);
            if (descriptor == null)
                return false;

            var depthPosition = (Vector3)depthPositionField.GetValue(descriptor);
            var depthRotation = (Quaternion)depthRotationField.GetValue(descriptor);
            var worldToTracking = (Matrix4x4)trackingSpaceWorldToLocalMethod.Invoke(
                environmentDepthManager, null);

            // The descriptor pose is expressed in the tracking-space
            // coordinate system. Convert it to the Unity world coordinate
            // system used by the passthrough-camera pose.
            var trackingFromDepth = Matrix4x4.TRS(
                depthPosition, depthRotation, Vector3.one);
            var worldFromTracking = worldToTracking.inverse;
            var worldFromDepth = worldFromTracking * trackingFromDepth;

            depthPose = new Pose(
                worldFromDepth.GetColumn(3),
                Quaternion.Normalize(worldFromDepth.rotation));
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ColorDepthTF] Failed to read depth pose: {exception.Message}");
            return false;
        }
    }

    private bool TryInitializeReflection()
    {
        if (reflectionInitialized)
            return descriptorsField != null && depthPositionField != null &&
                   depthRotationField != null && trackingSpaceWorldToLocalMethod != null;

        reflectionInitialized = true;

        var managerType = typeof(EnvironmentDepthManager);
        descriptorsField = managerType.GetField(
            "frameDescriptors", BindingFlags.Instance | BindingFlags.NonPublic);
        trackingSpaceWorldToLocalMethod = managerType.GetMethod(
            "GetTrackingSpaceWorldToLocalMatrix",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (descriptorsField == null || trackingSpaceWorldToLocalMethod == null)
            return false;

        var descriptorArrayType = descriptorsField.FieldType.GetElementType();
        if (descriptorArrayType == null)
            return false;

        depthPositionField = descriptorArrayType.GetField(
            "createPoseLocation", BindingFlags.Instance | BindingFlags.NonPublic);
        depthRotationField = descriptorArrayType.GetField(
            "createPoseRotation", BindingFlags.Instance | BindingFlags.NonPublic);

        return depthPositionField != null && depthRotationField != null;
    }
}
