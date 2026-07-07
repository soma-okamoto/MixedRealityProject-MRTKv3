/*
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class IRM_SerectObjectPublisher : UnityPublisher<Float32MultiArray>
    {
        [SerializeField] private float[] pendingCoords = new float[0];
        [SerializeField] private float[] lastPublishedData;
        public GameObject Aligin;

        protected override void Start()
        {
            base.Start();
        }

        /// <summary>���ɑ�����W��ێ�����iPublish �͂��Ȃ��j</summary>
        public void SetCoords(float[] coords)
        {
            pendingCoords = coords;
        }

        /// <summary>�ێ����̍��W����x�������M����</summary>
        public void PublishSelectData()
        {
            var message = new Float32MultiArray { data = pendingCoords };
            Aligin.GetComponent<AlignToTarget>().enabled = true;
            Publish(message);
            lastPublishedData = pendingCoords;
        }
    }

}*/

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
public class IRM_SerectObjectPublisher : MonoBehaviour
{
[Header("ROS 2 Topic")]
[SerializeField] private string topicName = "/IRM_Select";

    [SerializeField] private float[] pendingCoords = new float[0];
    [SerializeField] private float[] lastPublishedData = new float[0];

    public GameObject Aligin;

    private ROSConnection ros;
    private Float32MultiArrayMsg message;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        // ROS 2: std_msgs/msg/Float32MultiArray
        ros.RegisterPublisher<Float32MultiArrayMsg>(topicName);

        message = new Float32MultiArrayMsg
        {
            layout = new MultiArrayLayoutMsg(),
            data = new float[0]
        };

        Debug.Log(
            $"[IRM_SerectObjectPublisher] ROS-TCP publisher registered: " +
            $"topic={topicName}, type=std_msgs/Float32MultiArray");
    }

    /// <summary>
    /// 次に送信する座標を保存する。ここでは Publish しない。
    /// </summary>
    public void SetCoords(float[] coords)
    {
        if (coords == null)
        {
            Debug.LogWarning(
                "[IRM_SerectObjectPublisher] SetCoords received null data.");

            pendingCoords = new float[0];
            return;
        }

        // 呼び出し元が後で配列を書き換えても値が変わらないよう複製する
        pendingCoords = (float[])coords.Clone();
    }

    /// <summary>
    /// 保存済み座標を /IRM_Select へ一度だけ送信する。
    /// </summary>
    public void PublishSelectData()
    {
        if (!isActiveAndEnabled)
        {
            Debug.Log(
                "[IRM_SerectObjectPublisher] disabled のため Publish しません");
            return;
        }

        if (ros == null)
        {
            Debug.LogWarning(
                "[IRM_SerectObjectPublisher] ROSConnection が未初期化です。");
            return;
        }

        if (message == null)
        {
            message = new Float32MultiArrayMsg
            {
                layout = new MultiArrayLayoutMsg(),
                data = new float[0]
            };
        }

        if (Aligin != null)
        {
            AlignToTarget alignToTarget =
                Aligin.GetComponent<AlignToTarget>();

            if (alignToTarget != null)
            {
                alignToTarget.enabled = true;
            }
            else
            {
                Debug.LogWarning(
                    "[IRM_SerectObjectPublisher] Aligin に AlignToTarget がありません。");
            }
        }
        else
        {
            Debug.LogWarning(
                "[IRM_SerectObjectPublisher] Aligin が未設定です。");
        }

        message.data = pendingCoords ?? new float[0];

        ros.Publish(topicName, message);

        lastPublishedData = (float[])message.data.Clone();

        Debug.Log(
            $"[IRM_SerectObjectPublisher] Published: " +
            $"{lastPublishedData.Length} values");
    }
}

}
