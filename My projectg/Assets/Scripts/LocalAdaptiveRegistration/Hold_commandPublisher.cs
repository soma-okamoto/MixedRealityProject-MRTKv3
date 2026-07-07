/*
using UnityEngine;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;

public class Hold_commandPublisher : UnityPublisher<std_msgs.String>
{
    [Header("Command")]
    [SerializeField] private string command = "Hold";

    [Header("State")]
    [SerializeField] private bool isHolding = false;

    private std_msgs.String message;

    protected override void Start()
    {
        base.Start();

        message = new std_msgs.String();
        message.data = command;
    }

    public void HoldStart()
    {
        isHolding = true;
        // Debug.Log("[Hold_commandPublisher] Hold Start");
    }

    public void HoldStop()
    {
        isHolding = false;
        // Debug.Log("[Hold_commandPublisher] Hold Stop");
    }

    private void Update()
    {
        if (!isHolding)
            return;

        if (message == null)
        {
            message = new std_msgs.String();
        }

        message.data = command;
        Publish(message);

        // Debug.Log($"[Hold_commandPublisher] Published: {command}");
    }
}
*/



using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class Hold_commandPublisher : MonoBehaviour
{
    [Header("ROS 2 Topic")]
    [SerializeField] private string topicName = "/Hold_command";

    [Header("Command")]
    [SerializeField] private string command = "Hold";

    [Header("State")]
    [SerializeField] private bool isHolding = false;

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
            $"[Hold_commandPublisher] ROS-TCP publisher registered: " +
            $"topic={topicName}, type=std_msgs/String");
    }

    public void HoldStart()
    {
        isHolding = true;
        Debug.Log("[Hold_commandPublisher] Hold Start");
    }

    public void HoldStop()
    {
        isHolding = false;
        Debug.Log("[Hold_commandPublisher] Hold Stop");
    }

    private void Update()
    {
        if (!isHolding || ros == null)
            return;

        if (message == null)
        {
            message = new StringMsg();
        }

        message.data = command;
        ros.Publish(topicName, message);
    }
}

