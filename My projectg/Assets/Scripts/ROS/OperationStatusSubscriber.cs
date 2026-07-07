
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class OperationStatusSubscriber : MonoBehaviour
{
    [Header("ROS 2 Topic")]
    public string TopicName = "/gool_state";

    [Header("Latest received state")]
    public bool messageData;

    private ROSConnection ros;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        // ROS 2: std_msgs/msg/Bool
        ros.Subscribe<BoolMsg>(TopicName, ReceiveMessage);

        Debug.Log(
            $"[OperationStatusSubscriber] ROS-TCP subscriber registered: " +
            $"topic={TopicName}, type=std_msgs/Bool");
    }

    private void ReceiveMessage(BoolMsg message)
    {
        if (message == null)
        {
            Debug.LogWarning("[OperationStatusSubscriber] Received null Bool message.");
            return;
        }

        messageData = message.data;
    }
}
