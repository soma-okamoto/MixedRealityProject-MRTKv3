
using System;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Robotics.ROSTCPConnector;

using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

// 既存Scene/Prefabのスクリプト参照を維持したい場合は、
// このnamespaceを一時的に残してよい。
// RosSharp本体への依存はなくなる。
namespace RosSharp.RosBridgeClient
{
    public class handPosePublisher : MonoBehaviour
    {
        [Header("ROS 2 Topic")]

        // ROS# UnityPublisher<T> 側の Topic 値をできるだけ引き継ぐ
        [FormerlySerializedAs("Topic")]
        public string TopicName = "/palm_pose";

        public string FrameId = "Unity";

        [Header("Input")]
        public handTracking handTracking;

        private ROSConnection ros;
        private PoseStampedMsg message;

        private void Start()
        {
            if (handTracking == null)
            {
                Debug.LogError("[handPosePublisher] handTracking is not assigned.");
                enabled = false;
                return;
            }

            // Scene上の ROSConnection を取得。
            // なければ ROSConnectionPrefab または自動生成Instanceを使う。
            ros = ROSConnection.GetOrCreateInstance();

            // Publisherは最初に1回だけ登録する。
            ros.RegisterPublisher<PoseStampedMsg>(TopicName);

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
                    orientation = new QuaternionMsg()
                }
            };

            Debug.Log(
                $"[handPosePublisher] ROS-TCP publisher registered: " +
                $"topic={TopicName}, type=geometry_msgs/PoseStamped, frame_id={FrameId}");
        }

        private void FixedUpdate()
        {
            if (ros == null || message == null || handTracking == null)
                return;

            UpdateMessage();
            ros.Publish(TopicName, message);
        }

        private void UpdateMessage()
        {
            Vector3 unityPosition = handTracking.GetHandPositionFromOrigin();
            Quaternion unityRotation = handTracking.GetHandRotationFromOrigin();

            message.header.frame_id = FrameId;
            SetWallClockStamp(message.header);

            // 既存ROS#版と同一の座標変換を維持する。
            message.pose.position.x = unityPosition.z;
            message.pose.position.y = -unityPosition.x;
            message.pose.position.z = unityPosition.y;

            // 既存ROS#版と同一の姿勢変換を維持する。
            message.pose.orientation.x = unityRotation.z;
            message.pose.orientation.y = -unityRotation.x;
            message.pose.orientation.z = unityRotation.y;
            message.pose.orientation.w = -unityRotation.w;
        }

        private static void SetWallClockStamp(HeaderMsg header)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            long sec = now.ToUnixTimeSeconds();
            long ticksWithinSecond = now.Ticks % TimeSpan.TicksPerSecond;
            uint nanosec = (uint)(ticksWithinSecond * 100L); // 1 tick = 100 ns

            header.stamp.sec = (int)sec;
            header.stamp.nanosec = nanosec;
        }
    }
}
