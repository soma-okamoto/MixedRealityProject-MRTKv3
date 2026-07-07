
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class Float32MultiSubscriber : MonoBehaviour
{
    [Header("ROS 2 Topic")]
    public string TopicName = "/mullti_command";

    [Header("Latest received data")]
    public float[] messageData = new float[5];

    private ROSConnection ros;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        // ROS 2: std_msgs/msg/Float32MultiArray
        ros.Subscribe<Float32MultiArrayMsg>(TopicName, ReceiveMessage);

        Debug.Log(
            $"[Float32MultiSubscriber] ROS-TCP subscriber registered: " +
            $"topic={TopicName}, type=std_msgs/Float32MultiArray");
    }

    private void ReceiveMessage(Float32MultiArrayMsg message)
    {
        if (message == null || message.data == null)
        {
            Debug.LogWarning("[Float32MultiSubscriber] Received null Float32MultiArray data.");
            return;
        }

        // ROS通信内部の配列をそのまま参照せず、Unity側の保持用に複製する。
        messageData = (float[])message.data.Clone();
    }
}


