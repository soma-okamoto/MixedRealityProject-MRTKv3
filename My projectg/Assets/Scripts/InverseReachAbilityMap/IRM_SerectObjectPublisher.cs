//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace RosSharp.RosBridgeClient
//{
//    public class IRM_SerectObjectPublisher : UnityPublisher<MessageTypes.Std.Float32MultiArray>
//    {
//        float[] data = new float[0];
//        [SerializeField] private SelectObject select;
//        [SerializeField] private float[] lastPublishedData;  // 最後に Publish したデータ

//        protected override void Start()
//        {
//            base.Start();
//        }

//        public void PublishSelectData()
//        {
//            MessageTypes.Std.Float32MultiArray message;
//            message = new MessageTypes.Std.Float32MultiArray();
//            data = select.IRM_SelectMessage();
//            message.data = data;
//            Publish(message);

//            lastPublishedData = data;
//        }
//    }


//}
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class IRM_SerectObjectPublisher : UnityPublisher<Float32MultiArray>
    {
        [SerializeField] private float[] pendingCoords = new float[0];
        [SerializeField] private float[] lastPublishedData;

        protected override void Start()
        {
            base.Start();
        }

        /// <summary>次に送る座標を保持する（Publish はしない）</summary>
        public void SetCoords(float[] coords)
        {
            pendingCoords = coords;
        }

        /// <summary>保持中の座標を一度だけ送信する</summary>
        public void PublishSelectData()
        {
            var message = new Float32MultiArray { data = pendingCoords };
            Publish(message);
            lastPublishedData = pendingCoords;
        }
    }

}

