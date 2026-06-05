using UnityEngine;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;


    public class PlaceCommandPublisher : UnityPublisher<std_msgs.String>
    {
        [Header("Command")]
        [SerializeField] private string command = "Place";

        private std_msgs.String message;

        protected override void Start()
        {
            base.Start();

            message = new std_msgs.String();
            message.data = command;
        }

        public void PublishPlace()
        {
            if (!isActiveAndEnabled)
            {
                Debug.Log("[PlaceCommandPublisher] disabled のため Publish しません");
                return;
            }


            if (message == null)
            {
                message = new std_msgs.String();
            }

            message.data = command;
            Publish(message);

            Debug.Log($"[PlaceCommandPublisher] Published: {command}");
        }
    }
