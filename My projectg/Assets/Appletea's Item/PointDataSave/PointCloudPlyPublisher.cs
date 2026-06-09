using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;


namespace RosSharp.RosBridgeClient
{
    public class PointCloudPlyPublisher : UnityPublisher<std_msgs.String>
    {

        protected override void Start()
        {
     
            base.Start();
        }

        public void PublishPointCloudAsPly(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
            {
                Debug.LogWarning("PointCloud is empty. Publish canceled.");
                return;
            }

            string plyText = ConvertPointsToPly(points);

            std_msgs.String message = new std_msgs.String
            {
                data = plyText
            };

            Publish(message);

            Debug.Log($"Published PLY point cloud. points={points.Count}, chars={plyText.Length}");
        }

        private string ConvertPointsToPly(List<Vector3> points)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("ply");
            sb.AppendLine("format ascii 1.0");
            sb.AppendLine($"element vertex {points.Count}");
            sb.AppendLine("property float x");
            sb.AppendLine("property float y");
            sb.AppendLine("property float z");
            sb.AppendLine("end_header");

            foreach (Vector3 p in points)
            {
                sb.AppendLine($"{p.x} {p.y} {p.z}");
            }

            return sb.ToString();
        }
    }
}