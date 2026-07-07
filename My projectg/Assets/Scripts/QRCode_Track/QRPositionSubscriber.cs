/*
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class QRPositionSubscriber : UnitySubscriber<MessageTypes.Geometry.PoseStamped>
    {
        public Vector3 messageUnityPosition;
        public Vector3 messageRosPosition;
        public Quaternion messageUnityRotation;

        private bool isMessageReceived;

        public bool IsMessageReceived => isMessageReceived;

        protected override void Start()
        {
            base.Start();
        }

        protected override void ReceiveMessage(MessageTypes.Geometry.PoseStamped message)
        {
            messageRosPosition = new Vector3(
                (float)message.pose.position.x,
                (float)message.pose.position.y,
                (float)message.pose.position.z
            );

            messageUnityPosition = GetPosition(message);
            messageUnityRotation = GetRotation(message);

            isMessageReceived = true;
        }

        private Vector3 GetPosition(MessageTypes.Geometry.PoseStamped message)
        {
            Vector3 pRosQR = new Vector3(
                (float)message.pose.position.x,
                (float)message.pose.position.y,
                (float)message.pose.position.z
            );

            return ConvertOpenCVQRToMRUKQR(pRosQR);
        }

        private Quaternion GetRotation(MessageTypes.Geometry.PoseStamped message)
        {
            Quaternion qRos = new Quaternion(
                (float)message.pose.orientation.x,
                (float)message.pose.orientation.y,
                (float)message.pose.orientation.z,
                (float)message.pose.orientation.w
            );

            Vector3 rightRos = qRos * Vector3.right;
            Vector3 upRos = qRos * Vector3.up;
            Vector3 forwardRos = qRos * Vector3.forward;


            Vector3 rightUnity = ConvertOpenCVQRToMRUKQR(-rightRos);
            Vector3 upUnity = ConvertOpenCVQRToMRUKQR(upRos);
            Vector3 forwardUnity = ConvertOpenCVQRToMRUKQR(forwardRos);

            forwardUnity = Vector3.Cross(rightUnity, upUnity).normalized;
            upUnity = Vector3.Cross(forwardUnity, rightUnity).normalized;

            return Quaternion.LookRotation(forwardUnity, upUnity);
        }

        private Vector3 ConvertOpenCVQRToMRUKQR(Vector3 v)
        {
            return new Vector3(
                -v.x,
                 v.y,
                 v.z
            );
        }
    }
}*/

using UnityEngine;
using UnityEngine.Serialization;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

namespace RosSharp.RosBridgeClient
{
public class QRPositionSubscriber : MonoBehaviour
{
[Header("ROS 2 Topic")]
[FormerlySerializedAs("Topic")]
[SerializeField] private string topicName = "/QRposition";

    public Vector3 messageUnityPosition;
    public Vector3 messageRosPosition;
    public Quaternion messageUnityRotation;

    private bool isMessageReceived;
    public bool IsMessageReceived => isMessageReceived;

    private ROSConnection ros;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        ros.Subscribe<PoseStampedMsg>(topicName, ReceiveMessage);

        Debug.Log(
            $"[QRPositionSubscriber] ROS-TCP subscriber registered: " +
            $"topic={topicName}, type=geometry_msgs/PoseStamped");
    }

    private void ReceiveMessage(PoseStampedMsg message)
    {
        if (message == null || message.pose == null)
        {
            Debug.LogWarning("[QRPositionSubscriber] PoseStamped message is null.");
            return;
        }

        messageRosPosition = new Vector3(
            (float)message.pose.position.x,
            (float)message.pose.position.y,
            (float)message.pose.position.z
        );

        messageUnityPosition = GetPosition(message);
        messageUnityRotation = GetRotation(message);

        isMessageReceived = true;
    }

    private Vector3 GetPosition(PoseStampedMsg message)
    {
        Vector3 pRosQR = new Vector3(
            (float)message.pose.position.x,
            (float)message.pose.position.y,
            (float)message.pose.position.z
        );

        return ConvertOpenCVQRToMRUKQR(pRosQR);
    }

    private Quaternion GetRotation(PoseStampedMsg message)
    {
        Quaternion qRos = new Quaternion(
            (float)message.pose.orientation.x,
            (float)message.pose.orientation.y,
            (float)message.pose.orientation.z,
            (float)message.pose.orientation.w
        );

        Vector3 rightRos = qRos * Vector3.right;
        Vector3 upRos = qRos * Vector3.up;
        Vector3 forwardRos = qRos * Vector3.forward;

        Vector3 rightUnity = ConvertOpenCVQRToMRUKQR(-rightRos);
        Vector3 upUnity = ConvertOpenCVQRToMRUKQR(upRos);
        Vector3 forwardUnity = ConvertOpenCVQRToMRUKQR(forwardRos);

        forwardUnity = Vector3.Cross(rightUnity, upUnity).normalized;
        upUnity = Vector3.Cross(forwardUnity, rightUnity).normalized;

        return Quaternion.LookRotation(forwardUnity, upUnity);
    }

    private static Vector3 ConvertOpenCVQRToMRUKQR(Vector3 v)
    {
        return new Vector3(
            -v.x,
             v.y,
             v.z
        );
    }
}

}
