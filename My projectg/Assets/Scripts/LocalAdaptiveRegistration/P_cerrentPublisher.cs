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

        Debug.Log(
            $"[P_currentPublisher] Publish: ID={id}, " +
            $"pos=({message.data[1]}, {message.data[2]}, {message.data[3]})"
        );
    }
}
