
/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
    // Int32 メッセージを受け取って bottle_id フィールドに格納するサブスクライバ
    public class DetectBottle_score_subscriber : UnitySubscriber<Float32>
    {
        // 受信したボトルIDを格納する変数
        public float bottle_score ;
    

        protected override void Start()
        {
            base.Start();
        }

        // メッセージ受信時に呼ばれるコールバック
        protected override void ReceiveMessage(Float32 message)
        {
            bottle_score = message.data;
            
        }
    }
}
*/

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
    // std_msgs/msg/Float32 を受信して bottle_score に保存する
    public class DetectBottle_score_subscriber : MonoBehaviour
    {
        [Header("ROS 2 Topic")]
        [SerializeField] private string topicName = "/identified_bottle_score";

        [Header("Latest detected bottle score")]
        public float bottle_score;

        private ROSConnection ros;

        private void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

            // ROS 2: std_msgs/msg/Float32
            ros.Subscribe<Float32Msg>(topicName, ReceiveMessage);

            Debug.Log(
                $"[DetectBottle_score_subscriber] ROS-TCP subscriber registered: " +
                $"topic={topicName}, type=std_msgs/Float32");
        }

        private void ReceiveMessage(Float32Msg message)
        {
            if (message == null)
            {
                Debug.LogWarning(
                    "[DetectBottle_score_subscriber] Received null Float32 message.");
                return;
            }

            bottle_score = message.data;
        }
    }
}

