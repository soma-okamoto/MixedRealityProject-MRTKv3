/*
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// /youbot/offset トピックを購読し、ROS ↔ Unity の座標系差を考慮して
/// OriginObject を初期位置からの移動量分だけ動かすコンポーネント。
/// </summary>
public class YoubotOffsetSubscriber : UnitySubscriber<Vector3Stamped>
{
  

    [Tooltip("移動量を適用する Origin オブジェクト")]
    public GameObject OriginObject;
  

    // Unity 上で最初に設定されていた OriginObject のローカル位置
    private UnityEngine.Vector3 unityStartPos;

    // ROS オフセット（Unity 用）の初期値（最初に受信した値）
    private UnityEngine.Vector3 rosUnityOffset0;
    private bool isRosInit = false;
    public UnityEngine.Vector3 BaseMovePosition;
    /// <summary>
    /// 初期化時に OriginObject の開始位置をキャプチャ
    /// </summary>
    protected override void Start()
    {
        base.Start();
        
    }

    /// <summary>
    /// ROS メッセージを受信したときに呼ばれる
    /// </summary>
    protected override void ReceiveMessage(Vector3Stamped message)
    {
        // ROS の右手系データを Unity 左手系に変換: x'=-x, y'=z, z'=y
        // UnityEngine.Vector3 rosUnityOffset = new UnityEngine.Vector3(
        //     -(float)message.vector.x,
        //     (float)message.vector.z,
        //     -(float)message.vector.y
        // );

                //Amir用に
        UnityEngine.Vector3 rosUnityOffset = new UnityEngine.Vector3(
            -(float)message.vector.y,
            (float)message.vector.z,
            (float)message.vector.x
        );

      
        // OriginObject を移動指定
        BaseMovePosition = rosUnityOffset;
        
    }

}
*/
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class YoubotOffsetSubscriber : MonoBehaviour
{
[Header("ROS 2 Topic")]
[SerializeField] private string topicName = "/youbot/offset";

[Tooltip("移動量を適用する Origin オブジェクト")]
public GameObject OriginObject;

[Header("Debug")]
public Vector3 BaseMovePosition;

private ROSConnection ros;

// Unity上で最初に設定されていたOriginObjectのローカル位置
private Vector3 unityStartLocalPos;

// 最初に受信したROS offsetを基準値として保持
private Vector3 rosUnityOffset0;
private bool isRosInit;

private Vector3 latestRosUnityOffset;
private bool hasNewOffset;

private void Start()
{
    if (OriginObject == null)
    {
        Debug.LogError("[YoubotOffsetSubscriber] OriginObject が未設定です。");
        enabled = false;
        return;
    }

    unityStartLocalPos = OriginObject.transform.localPosition;

    ros = ROSConnection.GetOrCreateInstance();

    // ROS 2: geometry_msgs/msg/Vector3Stamped
    ros.Subscribe<Vector3StampedMsg>(topicName, ReceiveMessage);

    Debug.Log(
        $"[YoubotOffsetSubscriber] ROS-TCP subscriber registered: " +
        $"topic={topicName}, type=geometry_msgs/Vector3Stamped");
}

private void Update()
{
    if (!hasNewOffset || OriginObject == null)
        return;

    hasNewOffset = false;

    // 初回受信値を原点として扱う
    if (!isRosInit)
    {
        rosUnityOffset0 = latestRosUnityOffset;
        isRosInit = true;

        OriginObject.transform.localPosition = unityStartLocalPos;
        return;
    }

    // 初回値からの相対移動量
    Vector3 relativeOffset = latestRosUnityOffset - rosUnityOffset0;

    // OriginObjectを初期ローカル位置から移動
    OriginObject.transform.localPosition =
        unityStartLocalPos + relativeOffset;
}

private void ReceiveMessage(Vector3StampedMsg message)
{
    if (message == null || message.vector == null)
    {
        Debug.LogWarning(
            "[YoubotOffsetSubscriber] Received null Vector3Stamped message.");
        return;
    }

    // 元コードのAMIR用変換を維持
    // ROS:   (x, y, z)
    // Unity: (-y, z, x)
    Vector3 rosUnityOffset = new Vector3(
        -(float)message.vector.y,
        (float)message.vector.z,
        (float)message.vector.x
    );

    BaseMovePosition = rosUnityOffset;
    latestRosUnityOffset = rosUnityOffset;
    hasNewOffset = true;
}

public void ResetOffsetReference()
{
    isRosInit = false;

    if (OriginObject != null)
    {
        unityStartLocalPos = OriginObject.transform.localPosition;
    }

    Debug.Log("[YoubotOffsetSubscriber] Offset reference reset.");
}


}
