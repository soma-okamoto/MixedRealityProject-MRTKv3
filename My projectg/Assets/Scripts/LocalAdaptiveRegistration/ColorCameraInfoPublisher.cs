using System;
using PassthroughCameraSamples;
using RosSharp.RosBridgeClient;
using sensor_msgs = RosSharp.RosBridgeClient.MessageTypes.Sensor;
using UnityEngine;

/// <summary>
/// Publishes the intrinsic parameters of the Passthrough Camera as
/// sensor_msgs/CameraInfo.
///
/// The Meta camera intrinsics are defined for the camera's native calibration
/// resolution. They are scaled to the output resolution used by the color
/// image publisher before being sent to ROS.
/// </summary>
public class ColorCameraInfoPublisher : UnityPublisher<sensor_msgs.CameraInfo>
{
    [Header("Passthrough Camera")]
    [SerializeField]
    private PassthroughCameraEye cameraEye = PassthroughCameraEye.Left;

    [Header("ROS CameraInfo")]
    [SerializeField]
    private string frameId = "quest_color_left_optical";

    [SerializeField]
    private int outputWidth = 640;

    [SerializeField]
    private int outputHeight = 640;

    [SerializeField]
    private string distortionModel = "plumb_bob";

    public PassthroughCameraEye CameraEye => cameraEye;

    protected override void Start()
    {
        if (string.IsNullOrWhiteSpace(Topic))
            Topic = "/meta_color_camera_info";

        base.Start();
    }

    /// <summary>
    /// Publishes one CameraInfo message using the current Left camera
    /// calibration. Call this immediately before a color capture request.
    /// </summary>
    public void PublishCameraInfo()
    {
        try
        {
            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(cameraEye);
            var width = outputWidth > 0 ? outputWidth : intrinsics.Resolution.x;
            var height = outputHeight > 0 ? outputHeight : intrinsics.Resolution.y;

            if (intrinsics.Resolution.x <= 0 || intrinsics.Resolution.y <= 0)
            {
                Debug.LogError("[ColorCameraInfo] Meta camera calibration resolution is invalid.");
                return;
            }

            var scaleX = (double)width / intrinsics.Resolution.x;
            var scaleY = (double)height / intrinsics.Resolution.y;

            var fx = intrinsics.FocalLength.x * scaleX;
            var fy = intrinsics.FocalLength.y * scaleY;
            var cx = intrinsics.PrincipalPoint.x * scaleX;
            var cy = intrinsics.PrincipalPoint.y * scaleY;
            var skew = intrinsics.Skew * scaleX;

            var k = new[]
            {
                fx, skew, cx,
                0.0, fy, cy,
                0.0, 0.0, 1.0
            };

            var r = new[]
            {
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0
            };

            var p = new[]
            {
                fx, skew, cx, 0.0,
                0.0, fy, cy, 0.0,
                0.0, 0.0, 1.0, 0.0
            };

            var message = new sensor_msgs.CameraInfo
            {
                height = (uint)height,
                width = (uint)width,
                distortion_model = distortionModel,
                D = new double[5],
                K = k,
                R = r,
                P = p,
                binning_x = 0,
                binning_y = 0
            };

            message.header.frame_id = frameId;
            message.header.Update();
            Publish(message);

            Debug.Log($"[ColorCameraInfo] Published: eye={cameraEye}, " +
                      $"calibration={intrinsics.Resolution.x}x{intrinsics.Resolution.y}, " +
                      $"output={width}x{height}, fx={fx:F3}, fy={fy:F3}, " +
                      $"cx={cx:F3}, cy={cy:F3}, topic={Topic}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ColorCameraInfo] Failed to read Meta camera intrinsics: {exception.Message}");
        }
    }
}
