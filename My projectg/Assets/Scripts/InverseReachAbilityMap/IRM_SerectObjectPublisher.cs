using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class IRM_SerectObjectPublisher : UnityPublisher<MessageTypes.Std.Float32MultiArray>
    {
        float[] data = new float[0];
        [SerializeField] private SelectObject select;
        [SerializeField] private float[] lastPublishedData;  // ç≈å„Ç… Publish ÇµÇΩÉfÅ[É^

        protected override void Start()
        {
            base.Start();
        }

        public void PublishSelectData()
        {
            MessageTypes.Std.Float32MultiArray message;
            message = new MessageTypes.Std.Float32MultiArray();
            data = select.IRM_SelectMessage();
            message.data = data;
            Publish(message);

            lastPublishedData = data;
        }
    }


}

