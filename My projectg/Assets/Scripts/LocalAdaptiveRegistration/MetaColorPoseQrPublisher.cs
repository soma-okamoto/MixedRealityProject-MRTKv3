using RosSharp.RosBridgeClient;
using geometry_msgs = RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosTime = RosSharp.RosBridgeClient.MessageTypes.Std.Time;
using UnityEngine;

/// <summary>
/// Publishes the Meta left Color camera pose in the QR-code coordinate frame.
///
/// The QR frame is the Unity SharedAnchorRoot frame. The message is converted
/// to the ROS convention used by QRPositionSubscriber:
///   Unity QR position = (-ROS QR x, ROS QR y, ROS QR z)
///
/// The pose is published on a private topic and is intentionally not sent to
/// /tf, so it does not mix with the robot TF tree.
/// </summary>
public class MetaColorPoseQrPublisher : UnityPublisher<geometry_msgs.PoseStamped>
{
    [Header("ROS topic and frames")]
    [SerializeField]
    private string qrFrameId = "qr_main";

    [SerializeField]
    private string colorFrameId = "quest_color_left_optical";

    protected override void Start()
    {
        if (string.IsNullOrWhiteSpace(Topic))
            Topic = "/meta_color_pose_qr";

        base.Start();
    }

    /// <summary>
    /// Publishes the current Meta left Color camera pose relative to the QR
    /// SharedAnchorRoot. Call this from the same capture trigger as the images.
    /// </summary>
    public void PublishPose()
    {
        PublishPose(null);
    }

    /// <summary>
    /// Publishes the pose using the image timestamp that was captured by the
    /// Color image publisher.
    /// </summary>
    public void PublishPose(RosTime imageStamp)
    {
        if (SharedAnchorManager.Instance == null ||
            !SharedAnchorManager.Instance.IsInitialized ||
            SharedAnchorManager.Instance.SharedAnchorRoot == null)
        {
            Debug.LogWarning("[MetaColorPoseQR] SharedAnchorRoot is not ready.");
            return;
        }

        try
        {
            var colorPose = PassthroughCameraSamples.PassthroughCameraUtils
                .GetCameraPoseInWorld(PassthroughCameraSamples.PassthroughCameraEye.Left);
            var anchor = SharedAnchorManager.Instance.SharedAnchorRoot;

            var colorPositionInUnityQr = anchor.InverseTransformPoint(colorPose.position);
            var colorRotationInUnityQr =
                Quaternion.Inverse(anchor.rotation) * colorPose.rotation;

            // Unity QR coordinates and the ROS QR coordinates used by the
            // existing QRPositionSubscriber differ by an X-axis reflection.
            // Unity Color camera coordinates use Y-up, while ROS optical
            // coordinates use Y-down.
            var unityQrToRosQr = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
            var unityColorToRosOptical = Matrix4x4.Scale(new Vector3(1f, -1f, 1f));
            var qrFromColorUnity = Matrix4x4.TRS(
                colorPositionInUnityQr,
                colorRotationInUnityQr,
                Vector3.one);
            var qrFromColorRos =
                unityQrToRosQr * qrFromColorUnity * unityColorToRosOptical;

            var position = qrFromColorRos.GetColumn(3);
            var rotation = Quaternion.Normalize(qrFromColorRos.rotation);

            var message = new geometry_msgs.PoseStamped();
            message.header.frame_id = qrFrameId;
            message.pose.position = new geometry_msgs.Point(
                position.x, position.y, position.z);
            message.pose.orientation = new geometry_msgs.Quaternion(
                rotation.x, rotation.y, rotation.z, rotation.w);
            if (imageStamp == null)
            {
                message.header.Update();
            }
            else
            {
                message.header.stamp = new RosTime(imageStamp.secs, imageStamp.nsecs);
            }

            Publish(message);

            Debug.Log($"[MetaColorPoseQR] Published {qrFrameId} <- {colorFrameId}: " +
                      $"position=({position.x:F4}, {position.y:F4}, {position.z:F4}), " +
                      $"rotation=({rotation.x:F4}, {rotation.y:F4}, " +
                      $"{rotation.z:F4}, {rotation.w:F4}), topic={Topic}");
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[MetaColorPoseQR] Failed to publish camera pose: {exception.Message}");
        }
    }
}
