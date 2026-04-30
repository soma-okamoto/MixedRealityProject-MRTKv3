using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class QRPositionSubscriber : UnitySubscriber<MessageTypes.Geometry.PoseStamped>
    {
        public Vector3 messageUnityPosition;
        public Vector3 messageRosPosition;
        public Quaternion messageUnityRotation;

        private bool isMessageReceived;

        public bool IsMessageReceived
        {
            get { return isMessageReceived; }
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void ReceiveMessage(MessageTypes.Geometry.PoseStamped message)
        { 
            messageRosPosition.x = (float)message.pose.position.x;
            messageRosPosition.y = (float)message.pose.position.y;
            messageRosPosition.z = (float)message.pose.position.z;

            messageUnityPosition = GetPosition(message);
            messageUnityRotation = GetRotation(message);

            isMessageReceived = true;
        }

        private Vector3 GetPosition(MessageTypes.Geometry.PoseStamped message)
        {
            return new Vector3(
                -(float)message.pose.position.x,
                (float)message.pose.position.z,
                (float)message.pose.position.y
            );
        }

        private Quaternion GetRotation(MessageTypes.Geometry.PoseStamped message)
        {
            return new Quaternion(
                -(float)message.pose.orientation.x,
                (float)message.pose.orientation.y,
                (float)message.pose.orientation.z,
                (float)message.pose.orientation.w
            );
        }
    }
}