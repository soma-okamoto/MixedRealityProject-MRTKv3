
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class airTapPublisher : MonoBehaviour
{
    [Header("ROS 2 Topic")]
    public string TopicName = "/grasp_command";

    [Header("Input")]
    public airTap_distance distance;

    [Header("Debug")]
    public string outputdata;

    private ROSConnection ros;
    private StringMsg grip;

    private void Start()
    {
        if (distance == null)
        {
            Debug.LogError("[airTapPublisher] airTap_distance is not assigned.");
            enabled = false;
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();

        // std_msgs/msg/String publisher を一度だけ登録
        ros.RegisterPublisher<StringMsg>(TopicName);

        grip = new StringMsg(string.Empty);

        Debug.Log(
            $"[airTapPublisher] ROS-TCP publisher registered: " +
            $"topic={TopicName}, type=std_msgs/String");
    }

    private void FixedUpdate()
    {
        if (ros == null || grip == null || distance == null)
            return;

        outputdata = distance.bool2string();

        grip.data = outputdata;
        ros.Publish(TopicName, grip);
    }
}

