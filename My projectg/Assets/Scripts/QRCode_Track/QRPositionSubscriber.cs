using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class QRPositionSubscriber : UnitySubscriber<MessageTypes.Geometry.PoseStamped>
    {
        public Vector3 messagePosition;
        public Quaternion messageRotation;

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
            messagePosition = GetPosition(message);
            messageRotation = GetRotation(message);

            isMessageReceived = true;
        }

        private Vector3 GetPosition(MessageTypes.Geometry.PoseStamped message)
        {
            return new Vector3(
                -(float)message.pose.position.x,
                (float)message.pose.position.z,
                -(float)message.pose.position.y
            );
        }

        private Quaternion GetRotation(MessageTypes.Geometry.PoseStamped message)
        {
            return new Quaternion(
                (float)message.pose.orientation.z,
                -(float)message.pose.orientation.x,
                (float)message.pose.orientation.y,
                -(float)message.pose.orientation.w
            );
        }
    }
}