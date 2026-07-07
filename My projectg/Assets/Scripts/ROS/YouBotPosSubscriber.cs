
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

namespace RosSharp.RosBridgeClient
{
    public class YouBotPosSubscriber : MonoBehaviour
    {
        [Header("ROS 2 Topic")]
        [FormerlySerializedAs("Topic")]
        public string TopicName = "/YouBot_Position";

        [Header("Latest received pose in Unity coordinates")]
        public Vector3 messagePosition;
        public Quaternion messageRotation;

        private ROSConnection ros;

        private void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

            // ROS 2: geometry_msgs/msg/PoseStamped
            ros.Subscribe<PoseStampedMsg>(TopicName, ReceiveMessage);

            Debug.Log(
                $"[YouBotPosSubscriber] ROS-TCP subscriber registered: " +
                $"topic={TopicName}, type=geometry_msgs/PoseStamped");
        }

        private void OnDestroy()
        {
            // この topic をこのスクリプトだけが Subscribe している前提。
            if (ros != null)
            {
                ros.Unsubscribe(TopicName);
            }
        }

        private void ReceiveMessage(PoseStampedMsg message)
        {
            if (message == null)
            {
                Debug.LogWarning("[YouBotPosSubscriber] PoseStamped message is null.");
                return;
            }

            messagePosition = GetPosition(message);
            messageRotation = GetRotation(message);
        }

        private static Vector3 GetPosition(PoseStampedMsg message)
        {
            // 旧ROS#版と完全に同一の変換を維持
            // ROS:   (x, y, z)
            // Unity: (-y, z, x)
            return new Vector3(
                -(float)message.pose.position.y,
                (float)message.pose.position.z,
                (float)message.pose.position.x
            );
        }

        private static Quaternion GetRotation(PoseStampedMsg message)
        {
            // 旧ROS#版と完全に同一の変換を維持
            return new Quaternion(
                (float)message.pose.orientation.z,
                (float)-message.pose.orientation.x,
                (float)message.pose.orientation.y,
                (float)-message.pose.orientation.w
            );
        }
    }
}

