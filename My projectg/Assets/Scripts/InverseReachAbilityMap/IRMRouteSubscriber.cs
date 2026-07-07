/*
using RosSharp.RosBridgeClient.MessageTypes.Nav;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; 

namespace RosSharp.RosBridgeClient
{
    public class IRMRouteSubscriber : UnitySubscriber<Path>
    {
        public Path messagePath;
        public bool isDirty = false;

        private List<GameObject> instantiatedObjects = new List<GameObject>(); // ���������I�u�W�F�N�g���Ǘ�

        protected override void Start()
        {
            base.Start();
        }

        protected override void ReceiveMessage(Path message)
        {
            messagePath = message;
            Debug.Log("Received IRMPath Message");
            isDirty = true; // ← 新規受信の印

        }

        
    }
}
*/

using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

namespace RosSharp.RosBridgeClient
{
    public class IRMRouteSubscriber : MonoBehaviour
    {
        [Header("ROS 2 Topic")]
        [SerializeField] private string topicName = "/IRM_first_Route";

        [Header("Latest received route")]
        public PathMsg messagePath;
        public bool isDirty = false;

        // 既存の経路可視化Objectを管理するためのリスト。
        // 現時点ではこのスクリプト内で未使用。
        private readonly List<GameObject> instantiatedObjects =
            new List<GameObject>();

        private ROSConnection ros;

        private void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

            // ROS 2: nav_msgs/msg/Path
            ros.Subscribe<PathMsg>(topicName, ReceiveMessage);

            Debug.Log(
                $"[IRMRouteSubscriber] ROS-TCP subscriber registered: " +
                $"topic={topicName}, type=nav_msgs/Path");
        }

        private void ReceiveMessage(PathMsg message)
        {
            if (message == null)
            {
                Debug.LogWarning(
                    "[IRMRouteSubscriber] Received null Path message.");
                return;
            }

            messagePath = message;
            isDirty = true;

            int poseCount = message.poses != null ? message.poses.Length : 0;

            Debug.Log(
                $"[IRMRouteSubscriber] Received route: {poseCount} waypoints");
        }
    }
}

