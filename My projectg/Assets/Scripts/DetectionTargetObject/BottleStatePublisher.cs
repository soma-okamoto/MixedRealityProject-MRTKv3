
/*
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;

public class BottleStatePublisher : UnityPublisher<Float32MultiArray>
{
    [Tooltip("SignalManager をセットしてください")]
    public BottleSignalManager BottleSignalManager;

    private Float32MultiArray message;

    protected override void Start()
    {
        base.Start();
        message = new Float32MultiArray
        {
            layout = new MultiArrayLayout
            {
                dim = new[]
                {
                    new MultiArrayDimension { label = "bottles", size = 0, stride = 0 },
                    new MultiArrayDimension { label = "fields",  size = 9, stride = 9 }
                },
                data_offset = 0
            },
            data = new float[0]
        };
    }

    private void FixedUpdate()
    {
        PublishBottleStates();
    }

    public void PublishBottleStates()
    {
        // ← Use the 'signals' instance, not the type!
        var infos = BottleSignalManager.signals;
        int n     = infos.Count;
        int total = n * 9;

        // 1) data 配列を作る
        float[] data = new float[total];
        for (int i = 0; i < n; i++)
        {
            var info = infos[i];
            data[i * 9 + 0] = info.bottleID;    // your ID field
            data[i * 9 + 1] = info.position.x;
            data[i * 9 + 2] = info.position.y;
            data[i * 9 + 3] = info.position.z;
            data[i * 9 + 4] = info.insideFlag;
            data[i * 9 + 5] = info.s_touch;
            data[i * 9 + 6] = info.s_hand;
            data[i * 9 + 7] = info.s_head;
            data[i * 9 + 8] = info.s_accel;
        }

        // 2) layout を更新
        message.layout.dim[0].size   = (uint)n;
        message.layout.dim[0].stride = (uint)total;
        // dim[1] は固定 (9)

        // 3) data をセットして Publish
        message.data = data;
        Publish(message);
    }
}
*/

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class BottleStatePublisher : MonoBehaviour
{
    [Header("ROS 2 Topic")]
    [SerializeField] private string topicName = "/bottle_features";

    [Tooltip("SignalManager をセットしてください")]
    public BottleSignalManager BottleSignalManager;

    private ROSConnection ros;
    private Float32MultiArrayMsg message;

    private void Start()
    {
        if (BottleSignalManager == null)
        {
            Debug.LogError(
                "[BottleStatePublisher] BottleSignalManager が未設定です。");
            enabled = false;
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();

        // ROS 2: std_msgs/msg/Float32MultiArray
        ros.RegisterPublisher<Float32MultiArrayMsg>(topicName);

        message = new Float32MultiArrayMsg
        {
            layout = new MultiArrayLayoutMsg
            {
                dim = new[]
                {
                    new MultiArrayDimensionMsg
                    {
                        label = "bottles",
                        size = 0,
                        stride = 0
                    },
                    new MultiArrayDimensionMsg
                    {
                        label = "fields",
                        size = 9,
                        stride = 9
                    }
                },
                data_offset = 0
            },
            data = new float[0]
        };

        Debug.Log(
            $"[BottleStatePublisher] ROS-TCP publisher registered: " +
            $"topic={topicName}, type=std_msgs/Float32MultiArray");
    }

    private void FixedUpdate()
    {
        PublishBottleStates();
    }

    public void PublishBottleStates()
    {
        if (ros == null || message == null || BottleSignalManager == null)
            return;

        var infos = BottleSignalManager.signals;

        if (infos == null)
        {
            Debug.LogWarning(
                "[BottleStatePublisher] BottleSignalManager.signals が null です。");
            return;
        }

        int bottleCount = infos.Count;
        const int fieldsPerBottle = 9;
        int totalElements = bottleCount * fieldsPerBottle;

        float[] data = new float[totalElements];

        for (int i = 0; i < bottleCount; i++)
        {
            var info = infos[i];
            int offset = i * fieldsPerBottle;

            // data = [
            //   bottleID, x, y, z,
            //   insideFlag, s_touch, s_hand, s_head, s_accel
            // ]
            data[offset + 0] = info.bottleID;
            data[offset + 1] = info.position.x;
            data[offset + 2] = info.position.y;
            data[offset + 3] = info.position.z;
            data[offset + 4] = info.insideFlag;
            data[offset + 5] = info.s_touch;
            data[offset + 6] = info.s_hand;
            data[offset + 7] = info.s_head;
            data[offset + 8] = info.s_accel;
        }

        // layout shape: [bottleCount, 9]
        message.layout.dim[0].size = (uint)bottleCount;
        message.layout.dim[0].stride = (uint)totalElements;

        message.layout.dim[1].size = fieldsPerBottle;
        message.layout.dim[1].stride = fieldsPerBottle;

        message.data = data;

        ros.Publish(topicName, message);
    }
}
