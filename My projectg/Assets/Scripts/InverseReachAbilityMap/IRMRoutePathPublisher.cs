/*using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Nav;
using UnityEngine.UIElements;

public class IRMRoutePathPublisher : UnityPublisher<Path>
{
    public string FrameId = "Unity";
    private Path message;
    UnityEngine.Vector3 pose;
    public PoseStamped[] WayPointPoseList;
    public UnityEngine.Transform startTransform;

    public List<GameObject> WayPointObjectList;

    public bool PublishStatus;
    

    protected override void Start()
    {
        base.Start();
        InitializeMessage();

    }

    private void FixedUpdate()
    {
        if (PublishStatus)
        {
            GetWayPointList();
            UpdateMessage();
        }
    }
    private void InitializeMessage()
    {
        message = new Path
        {
            header = new RosSharp.RosBridgeClient.MessageTypes.Std.Header()
            {
                frame_id = FrameId
            }
            
        };
    }
    private void UpdateMessage()
    {
        message.header.Update();
        message.poses = WayPointPoseList;
        Debug.Log("Publish : " + message.poses.Length);
        Publish(message);
        PublishStatus = false;
    }

    private void GetWayPointList()
    {
        Array.Resize(ref WayPointPoseList, WayPointObjectList.Count);

        for(int i = 0; i < WayPointObjectList.Count; i++)
        {
            // RosSharp.RosBridgeClient.MessageTypes.Geometry.Point position = GetPosition(WayPointObjectList[i].transform.localPosition);
                                                                                                                                                    
            // ① ワールド座標を取得xxxx
            UnityEngine.Vector3 worldPos = WayPointObjectList[i].transform.position;
                        // ② startTransform が指定されていれば、そこからの相対位置に変換
            UnityEngine.Vector3 relPos = startTransform != null
                            ? worldPos - startTransform.position
                            : WayPointObjectList[i].transform.localPosition;
            // ③ Unity→ROS 座標変換
            Point position = GetPosition(relPos);

            RosSharp.RosBridgeClient.MessageTypes.Geometry.Quaternion orientation = GetRotation(WayPointObjectList[i].transform.localRotation);
            PoseStamped poseStamped = new PoseStamped();
            poseStamped.header = message.header;
            poseStamped.pose.position = position;
            poseStamped.pose.orientation = orientation;
            WayPointPoseList[i] = poseStamped;
        }
    }

    private RosSharp.RosBridgeClient.MessageTypes.Geometry.Point GetPosition(UnityEngine.Vector3 pos)
    {
        // ROS�̍��W�n����Unity�̍��W�n�֕ϊ��i�ʏ�AROS�͉E��n�AUnity�͍���n�j
        // return new RosSharp.RosBridgeClient.MessageTypes.Geometry.Point((float)-pos.x, -(float)pos.z, (float)pos.y);
         return new RosSharp.RosBridgeClient.MessageTypes.Geometry.Point((float)pos.z, -(float)pos.x, (float)pos.y);

    }

    private RosSharp.RosBridgeClient.MessageTypes.Geometry.Quaternion GetRotation(UnityEngine.Quaternion orientation)
    {
        // �l�����̕ϊ������l�ɍs���܂�
        return new RosSharp.RosBridgeClient.MessageTypes.Geometry.Quaternion((float)orientation.z, -(float)orientation.x, (float)orientation.y, -(float)orientation.w);
    }
}
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Robotics.ROSTCPConnector;

using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Std;

public class IRMRoutePathPublisher : MonoBehaviour
{
[Header("ROS 2 Topic")]
[FormerlySerializedAs("Topic")]
[SerializeField] private string topicName = "/IRM_Edit_Route";

public string FrameId = "Unity";

[Header("Route")]
public PoseStampedMsg[] WayPointPoseList;
public Transform startTransform;
public List<GameObject> WayPointObjectList = new List<GameObject>();

[Header("Publish Control")]
public bool PublishStatus;

private ROSConnection ros;
private PathMsg message;

private void Start()
{
    ros = ROSConnection.GetOrCreateInstance();

    ros.RegisterPublisher<PathMsg>(topicName);

    message = new PathMsg
    {
        header = new HeaderMsg
        {
            frame_id = FrameId,
            stamp = GetCurrentRosTime()
        },
        poses = Array.Empty<PoseStampedMsg>()
    };

    Debug.Log(
        $"[IRMRoutePathPublisher] ROS-TCP publisher registered: " +
        $"topic={topicName}, type=nav_msgs/Path");
}

private void FixedUpdate()
{
    if (!PublishStatus)
        return;

    if (ros == null || message == null)
        return;

    GetWayPointList();
    UpdateMessage();

    // 元コードと同じく1回publishしたら停止
    PublishStatus = false;
}

private void GetWayPointList()
{
    if (WayPointObjectList == null)
    {
        WayPointPoseList = Array.Empty<PoseStampedMsg>();
        return;
    }

    WayPointPoseList = new PoseStampedMsg[WayPointObjectList.Count];

    for (int i = 0; i < WayPointObjectList.Count; i++)
    {
        GameObject waypoint = WayPointObjectList[i];

        if (waypoint == null)
        {
            Debug.LogWarning(
                $"[IRMRoutePathPublisher] WayPointObjectList[{i}] is null.");

            WayPointPoseList[i] = CreatePoseStamped(
                Vector3.zero,
                Quaternion.identity
            );

            continue;
        }

        // ① waypoint のワールド座標を取得
        Vector3 worldPos = waypoint.transform.position;

        // ② startTransform があれば、その位置を原点とした相対座標に変換
        Vector3 relativePos = startTransform != null
            ? worldPos - startTransform.position
            : waypoint.transform.localPosition;

        // ③ 元コードと同じ座標変換を適用
        WayPointPoseList[i] = CreatePoseStamped(
            relativePos,
            waypoint.transform.localRotation
        );
    }
}

private PoseStampedMsg CreatePoseStamped(
    Vector3 unityPosition,
    Quaternion unityRotation)
{
    return new PoseStampedMsg
    {
        header = new HeaderMsg
        {
            frame_id = FrameId,
            stamp = GetCurrentRosTime()
        },
        pose = new PoseMsg
        {
            position = GetPosition(unityPosition),
            orientation = GetRotation(unityRotation)
        }
    };
}

private void UpdateMessage()
{
    message.header.frame_id = FrameId;
    message.header.stamp = GetCurrentRosTime();
    message.poses = WayPointPoseList ?? Array.Empty<PoseStampedMsg>();

    ros.Publish(topicName, message);

    Debug.Log(
        $"[IRMRoutePathPublisher] Published route: " +
        $"{message.poses.Length} waypoints");
}

private static PointMsg GetPosition(Vector3 pos)
{
    // 元コードの変換を維持
    // Unity relative position: (x, y, z)
    // ROS position:            (z, -x, y)
    return new PointMsg
    {
        x = pos.z,
        y = -pos.x,
        z = pos.y
    };
}

private static QuaternionMsg GetRotation(Quaternion orientation)
{
    // 元コードの姿勢変換を維持
    return new QuaternionMsg
    {
        x = orientation.z,
        y = -orientation.x,
        z = orientation.y,
        w = -orientation.w
    };
}

private static TimeMsg GetCurrentRosTime()
{
    DateTimeOffset now = DateTimeOffset.UtcNow;

    long seconds = now.ToUnixTimeSeconds();
    long ticksWithinSecond = now.Ticks % TimeSpan.TicksPerSecond;

    return new TimeMsg
    {
        sec = (int)seconds,
        nanosec = (uint)(ticksWithinSecond * 100L)
    };
}

}