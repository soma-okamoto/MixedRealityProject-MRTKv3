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
