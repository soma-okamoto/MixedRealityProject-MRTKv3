
/*
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class BaseMovePublisher : UnityPublisher<MessageTypes.Geometry.PoseStamped>
    {

        [SerializeField] private ObjectInfoSetting objectInfoSetting;


        public string FrameId = "Unity";
        private MessageTypes.Geometry.PoseStamped message;
        public bool MoveStatus = false;


        protected override void Start()
        {
            base.Start();
            InitializeMessage();

        }

        public void BaseMovePub()
        {

                UpdateMessage();
            

        }
        private void InitializeMessage()
        {
            message = new MessageTypes.Geometry.PoseStamped
            {
                header = new MessageTypes.Std.Header()
                {
                    frame_id = FrameId
                }
            };
        }

        private void UpdateMessage()
        {
            message.header.Update();
            if (objectInfoSetting != null)
            {
                Vector3 pose = objectInfoSetting.GetMovePosition();
                GetGeometryPoint(pose, message.pose.position);
            }
            Publish(message);



        }

        private static void GetGeometryPoint(Vector3 position, MessageTypes.Geometry.Point geometryPoint)
        {
            // geometryPoint.x = -position.x;
            // geometryPoint.y = -position.z;
            // geometryPoint.z = position.y;
            geometryPoint.x = position.z;
            geometryPoint.y = -position.x;
            geometryPoint.z = position.y;

        }

    }
}*/
using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
public class BaseMovePublisher : MonoBehaviour
{
[Header("ROS 2 Topic")]
[SerializeField] private string topicName = "/Base_Move";

    [SerializeField] private ObjectInfoSetting objectInfoSetting;

    public string FrameId = "Unity";
    public bool MoveStatus = false;

    private ROSConnection ros;
    private PoseStampedMsg message;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        ros.RegisterPublisher<PoseStampedMsg>(topicName);

        InitializeMessage();

        Debug.Log(
            $"[BaseMovePublisher] ROS-TCP publisher registered: " +
            $"topic={topicName}, type=geometry_msgs/PoseStamped");
    }

    public void BaseMovePub()
    {
        if (!isActiveAndEnabled)
        {
            Debug.Log("[BaseMovePublisher] disabled のため Publish しません");
            return;
        }

        if (ros == null || message == null)
        {
            Debug.LogWarning("[BaseMovePublisher] ROSConnection が未初期化です。");
            return;
        }

        if (objectInfoSetting == null)
        {
            Debug.LogWarning("[BaseMovePublisher] ObjectInfoSetting が未設定です。");
            return;
        }

        UpdateMessage();
    }

    private void InitializeMessage()
    {
        message = new PoseStampedMsg
        {
            header = new HeaderMsg
            {
                frame_id = FrameId,
                stamp = new TimeMsg()
            },
            pose = new PoseMsg
            {
                position = new PointMsg(),
                orientation = new QuaternionMsg
                {
                    w = 1.0
                }
            }
        };
    }

    private void UpdateMessage()
    {
        Vector3 unityPosition = objectInfoSetting.GetMovePosition();

        message.header.frame_id = FrameId;
        message.header.stamp = GetCurrentRosTime();

        GetGeometryPoint(unityPosition, message.pose.position);

        ros.Publish(topicName, message);

        Debug.Log(
            $"[BaseMovePublisher] Published: " +
            $"unity={unityPosition}, " +
            $"ros=({message.pose.position.x:F3}, " +
            $"{message.pose.position.y:F3}, " +
            $"{message.pose.position.z:F3})");
    }

    private static void GetGeometryPoint(
        Vector3 position,
        PointMsg geometryPoint)
    {
        // Unity: (x, y, z)
        // ROS:   (z, -x, y)
        geometryPoint.x = position.z;
        geometryPoint.y = -position.x;
        geometryPoint.z = position.y;
    }

    private static TimeMsg GetCurrentRosTime()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        long seconds = now.ToUnixTimeSeconds();
        long ticksWithinSecond = now.Ticks % TimeSpan.TicksPerSecond;

        return new TimeMsg
        {
            sec = (int)seconds,
            nanosec = (uint)(ticksWithinSecond * 100L)
        };
    }
}

}