using UnityEngine;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;



public class Hold_commandPublisher :  UnityPublisher<std_msgs.String>
{
 [Header("Command")]
        [SerializeField] private string command = "Hold";

        private std_msgs.String message;

        protected override void Start()
        {
            base.Start();

            message = new std_msgs.String();
            message.data = command;
        }

        public void Update()
        {
            if (message == null)
            {
                message = new std_msgs.String();
            }

            message.data = command;
            Publish(message);

            // Debug.Log($"[Hold_commandPublisher] Published: {command}");
        }
}
