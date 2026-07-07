/*
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;


namespace RosSharp.RosBridgeClient
{
    public class PointCloudPlyPublisher : UnityPublisher<std_msgs.Float32MultiArray>
    {

        protected override void Start()
        {

            base.Start();
        }

        public void PublishPointCloud(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
            {
                Debug.LogWarning("PointCloud is empty. Publish canceled.");
                return;
            }

            float[] data = new float[points.Count * 3];

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = points[i];

                int index = i * 3;

                // まずはUnity座標をそのまま送る
                data[index + 0] = p.x;
                data[index + 1] = p.y;
                data[index + 2] = p.z;
            }

            std_msgs.Float32MultiArray message = new std_msgs.Float32MultiArray
            {
                data = data
            };

            Publish(message);

            Debug.Log($"Published point cloud array. points={points.Count}, float_count={data.Length}");
        }
    }
}
*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
public class PointCloudPlyPublisher : MonoBehaviour
{
[Header("ROS 2 Topic")]
[FormerlySerializedAs("Topic")]
[SerializeField] private string topicName = "/Depth_savedata";

    private ROSConnection ros;
    private Float32MultiArrayMsg message;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        ros.RegisterPublisher<Float32MultiArrayMsg>(topicName);

        message = new Float32MultiArrayMsg
        {
            layout = new MultiArrayLayoutMsg(),
            data = new float[0]
        };

        Debug.Log(
            $"[PointCloudPlyPublisher] ROS-TCP publisher registered: " +
            $"topic={topicName}, type=std_msgs/Float32MultiArray");
    }

    public void PublishPointCloud(List<Vector3> points)
    {
        if (!isActiveAndEnabled)
        {
            Debug.Log(
                "[PointCloudPlyPublisher] disabled のため Publish しません");
            return;
        }

        if (ros == null)
        {
            Debug.LogWarning(
                "[PointCloudPlyPublisher] ROSConnection が未初期化です。");
            return;
        }

        if (points == null || points.Count == 0)
        {
            Debug.LogWarning(
                "[PointCloudPlyPublisher] PointCloud is empty. Publish canceled.");
            return;
        }

        float[] data = new float[points.Count * 3];

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = points[i];
            int index = i * 3;

            // Unity座標をそのまま格納
            data[index + 0] = p.x;
            data[index + 1] = p.y;
            data[index + 2] = p.z;
        }

        if (message == null)
        {
            message = new Float32MultiArrayMsg
            {
                layout = new MultiArrayLayoutMsg()
            };
        }

        message.data = data;

        ros.Publish(topicName, message);

        Debug.Log(
            $"[PointCloudPlyPublisher] Published point cloud: " +
            $"points={points.Count}, float_count={data.Length}");
    }
}

}