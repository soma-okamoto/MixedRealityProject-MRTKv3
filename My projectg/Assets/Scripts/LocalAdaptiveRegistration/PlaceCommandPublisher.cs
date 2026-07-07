/*
using UnityEngine;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;


    public class PlaceCommandPublisher : UnityPublisher<std_msgs.String>
    {
        [Header("Command")]
        [SerializeField] private string command = "Place";

        private std_msgs.String message;

        protected override void Start()
        {
            base.Start();

            message = new std_msgs.String();
            message.data = command;
        }

        public void PublishPlace()
        {
            if (!isActiveAndEnabled)
            {
                Debug.Log("[PlaceCommandPublisher] disabled のため Publish しません");
                return;
            }


            if (message == null)
            {
                message = new std_msgs.String();
            }

            message.data = command;
            Publish(message);

            Debug.Log($"[PlaceCommandPublisher] Published: {command}");
        }
    }
    */

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class PlaceCommandPublisher : MonoBehaviour
{
    [Header("ROS 2 Topic")]
    [SerializeField] private string topicName = "/place_command";

    [Header("Command")]
    [SerializeField] private string command = "Place";

    private ROSConnection ros;
    private StringMsg message;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        // ROS 2: std_msgs/msg/String
        ros.RegisterPublisher<StringMsg>(topicName);

        message = new StringMsg
        {
            data = command
        };

        Debug.Log(
            $"[PlaceCommandPublisher] ROS-TCP publisher registered: " +
            $"topic={topicName}, type=std_msgs/String");
    }

    public void PublishPlace()
    {
        if (!isActiveAndEnabled)
        {
            Debug.Log("[PlaceCommandPublisher] disabled のため Publish しません");
            return;
        }

        if (ros == null)
        {
            Debug.LogWarning("[PlaceCommandPublisher] ROSConnection が未初期化です。");
            return;
        }

        if (message == null)
        {
            message = new StringMsg();
        }

        message.data = command;
        ros.Publish(topicName, message);

        Debug.Log($"[PlaceCommandPublisher] Published: {command}");
    }
}

