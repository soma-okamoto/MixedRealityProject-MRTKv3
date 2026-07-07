/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
    // Int32 メッセージを受け取って bottle_id フィールドに格納するサブスクライバ
    public class DetectBottleSubscriber : UnitySubscriber<Int32>
    {
        // 受信したボトルIDを格納する変数
        public int bottle_id =-1;
    

        protected override void Start()
        {
            base.Start();
        }

        // メッセージ受信時に呼ばれるコールバック
        protected override void ReceiveMessage(Int32 message)
        {
            // Int32 型メッセージのデータは message.data に入っている
            bottle_id = message.data;
            //Debug.Log($"[DetectBottleSubscriber] Received bottle_id = {bottle_id}");
        }
    }
}
*/

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
    // std_msgs/msg/Int32 を受信して bottle_id に保存する
    public class DetectBottleSubscriber : MonoBehaviour
    {
        [Header("ROS 2 Topic")]
        [SerializeField] private string topicName = "/identified_bottle";

        [Header("Latest detected bottle ID")]
        public int bottle_id = -1;

        private ROSConnection ros;

        private void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

            // ROS 2: std_msgs/msg/Int32
            ros.Subscribe<Int32Msg>(topicName, ReceiveMessage);

            Debug.Log(
                $"[DetectBottleSubscriber] ROS-TCP subscriber registered: " +
                $"topic={topicName}, type=std_msgs/Int32");
        }

        private void ReceiveMessage(Int32Msg message)
        {
            if (message == null)
            {
                Debug.LogWarning(
                    "[DetectBottleSubscriber] Received null Int32 message.");
                return;
            }

            bottle_id = message.data;

            // Debug.Log(
            //     $"[DetectBottleSubscriber] Received bottle_id={bottle_id}");
        }
    }
}

