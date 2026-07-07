/*
using UnityEngine;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
    public class Phase_name_Subscriber : UnitySubscriber<std_msgs.String>
    {


        public string phase_name;
        private bool isMessageChange = false;

        protected override void Start()
        {
            base.Start(); // ROS�T�u�X�N���C�u�J�n
        }


        protected override void ReceiveMessage(std_msgs.String message)
        {
            phase_name = message.data;
            
        }

    }

}
*/

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
    public class Phase_name_Subscriber : MonoBehaviour
    {
        [Header("ROS 2 Topic")]
        [SerializeField] private string topicName = "/phase_name";

        [Header("Latest phase name")]
        public string phase_name;

        private ROSConnection ros;

        private void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

            // ROS 2: std_msgs/msg/String
            ros.Subscribe<StringMsg>(topicName, ReceiveMessage);

            Debug.Log(
                $"[Phase_name_Subscriber] ROS-TCP subscriber registered: " +
                $"topic={topicName}, type=std_msgs/String");
        }

        private void OnDestroy()
        {
            if (ros != null)
            {
                ros.Unsubscribe(topicName);
            }
        }

        private void ReceiveMessage(StringMsg message)
        {
            if (message == null)
            {
                Debug.LogWarning("[Phase_name_Subscriber] Received null String message.");
                return;
            }

            phase_name = message.data;
        }
    }
}
