
/*
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;


public class P_cerrentPublisher : UnityPublisher<Float32MultiArray>
{
    private Float32MultiArray message;

    protected override void Start()
    {
        base.Start();

        message = new Float32MultiArray();
        message.data = new float[4];
    }

    public void PublishCurrent(int id, float[] youbotPosition)
    {
        if (!isActiveAndEnabled)
        {
            Debug.Log("[P_currentPublisher] disabled のため Publish しません");
            return;
        }

        if (youbotPosition == null || youbotPosition.Length < 3)
        {
            Debug.LogWarning("[P_currentPublisher] youbotPosition が不正です");
            return;
        }

        message.data[0] = id;
        message.data[1] = youbotPosition[0];
        message.data[2] = youbotPosition[1];
        message.data[3] = youbotPosition[2];

        Publish(message);

        //Debug.Log(
        //    $"[P_currentPublisher] Publish: ID={id}, " +
        //    $"pos=({message.data[1]}, {message.data[2]}, {message.data[3]})"
        //);
        Debug.Log("P_currentPublisher: Publish");


    }
}
*/

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class P_cerrentPublisher : MonoBehaviour
{
    [Header("ROS 2 Topic")]
    [SerializeField] private string topicName = "/P_current";

    private ROSConnection ros;
    private Float32MultiArrayMsg message;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        // ROS 2: std_msgs/msg/Float32MultiArray
        ros.RegisterPublisher<Float32MultiArrayMsg>(topicName);

        message = new Float32MultiArrayMsg
        {
            layout = new MultiArrayLayoutMsg(),
            data = new float[4]
        };

        Debug.Log(
            $"[P_currentPublisher] ROS-TCP publisher registered: " +
            $"topic={topicName}, type=std_msgs/Float32MultiArray");
    }

    public void PublishCurrent(int id, float[] youbotPosition)
    {
        if (!isActiveAndEnabled)
        {
            Debug.Log("[P_currentPublisher] disabled のため Publish しません");
            return;
        }

        if (ros == null)
        {
            Debug.LogWarning("[P_currentPublisher] ROSConnection が未初期化です");
            return;
        }

        if (youbotPosition == null || youbotPosition.Length < 3)
        {
            Debug.LogWarning("[P_currentPublisher] youbotPosition が不正です");
            return;
        }

        if (message == null || message.data == null || message.data.Length != 4)
        {
            message = new Float32MultiArrayMsg
            {
                layout = new MultiArrayLayoutMsg(),
                data = new float[4]
            };
        }

        // data = [robot_id, x, y, z]
        message.data[0] = id;
        message.data[1] = youbotPosition[0];
        message.data[2] = youbotPosition[1];
        message.data[3] = youbotPosition[2];

        ros.Publish(topicName, message);

        Debug.Log(
            $"[P_currentPublisher] Publish: ID={id}, " +
            $"pos=({message.data[1]}, {message.data[2]}, {message.data[3]})");
    }
}

