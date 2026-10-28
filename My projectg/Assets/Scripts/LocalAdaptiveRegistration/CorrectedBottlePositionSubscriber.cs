using RosSharp.RosBridgeClient;
using geometry_msgs = RosSharp.RosBridgeClient.MessageTypes.Geometry;
using UnityEngine;

/// <summary>
/// Receives the final bottle position calculated by ROS.
/// The next PoseStamped received after ArmForBottle is applied only to that bottle.
/// </summary>
public class CorrectedBottlePositionSubscriber : UnitySubscriber<geometry_msgs.PoseStamped>
{
    [Header("ROS result")]
    [SerializeField]
    private string defaultTopic = "/P_place";

    [Header("Coordinate conversion")]
    [Tooltip("Unity ObjectList used as the parent/reference for bottle coordinates.")]
    [SerializeField]
    private Transform defaultObjectList;

    private readonly object messageLock = new object();

    private GameObject pendingBottle;
    private Transform pendingObjectList;
    private int pendingBottleId = -1;
    private bool waitingForResult;
    private bool resultReady;
    private Vector3 pendingRosPosition;
    private string pendingFrameId;

    protected override void Start()
    {
        if (string.IsNullOrWhiteSpace(Topic))
            Topic = defaultTopic;

        base.Start();
        Debug.Log($"[CorrectedBottlePosition] Subscriber started. topic={Topic}");
    }

    /// <summary>
    /// Arms this subscriber for one PLACE request.
    /// Call before publishing P_current and PLACE.
    /// </summary>
    public bool ArmForBottle(int bottleId, GameObject bottle, Transform objectListTransform = null)
    {
        if (bottle == null)
        {
            Debug.LogWarning("[CorrectedBottlePosition] Cannot arm: bottle is null.");
            return false;
        }

        var objectList = objectListTransform != null ? objectListTransform : defaultObjectList;
        if (objectList == null)
        {
            Debug.LogWarning("[CorrectedBottlePosition] Cannot arm: reference origin is not assigned.");
            return false;
        }

        lock (messageLock)
        {
            pendingBottleId = bottleId;
            pendingBottle = bottle;
            pendingObjectList = objectList;
            waitingForResult = true;
            resultReady = false;
            pendingRosPosition = Vector3.zero;
            pendingFrameId = string.Empty;
        }

        Debug.Log(
            $"[CorrectedBottlePosition] Waiting for ROS result. " +
            $"bottleId={bottleId}, bottle={bottle.name}, objectList={objectList.name}");
        return true;
    }

    public void CancelPendingResult()
    {
        lock (messageLock)
        {
            waitingForResult = false;
            resultReady = false;
            pendingBottle = null;
            pendingObjectList = null;
            pendingBottleId = -1;
        }
    }

    protected override void ReceiveMessage(geometry_msgs.PoseStamped message)
    {
        var rosPosition = new Vector3(
            (float)message.pose.position.x,
            (float)message.pose.position.y,
            (float)message.pose.position.z);

        lock (messageLock)
        {
            // Ignore unsolicited or stale results.
            if (!waitingForResult)
                return;

            pendingRosPosition = rosPosition;
            pendingFrameId = message.header != null ? message.header.frame_id : string.Empty;
            resultReady = true;
        }
    }

    private void Update()
    {
        GameObject bottle;
        Transform objectList;
        Vector3 rosPosition;
        string frameId;
        int bottleId;

        lock (messageLock)
        {
            if (!resultReady)
                return;

            bottle = pendingBottle;
            objectList = pendingObjectList;
            rosPosition = pendingRosPosition;
            frameId = pendingFrameId;
            bottleId = pendingBottleId;

            resultReady = false;
            waitingForResult = false;
            pendingBottle = null;
            pendingObjectList = null;
            pendingBottleId = -1;
        }

        if (bottle == null || objectList == null)
        {
            Debug.LogWarning(
                "[CorrectedBottlePosition] ROS result received, but the target " +
                "bottle or ObjectList no longer exists.");
            return;
        }

        // Existing conversion used by P_current and YouBotPosSubscriber:
        // ROS robot (x, y, z) -> Unity local (-x, z, y).
        var unityLocalPosition = new Vector3(
            -rosPosition.x,
            rosPosition.z,
            rosPosition.y);

        var unityWorldPosition = objectList.TransformPoint(unityLocalPosition);
        bottle.transform.position = unityWorldPosition;

        Debug.Log(
            $"[CorrectedBottlePosition] Applied ROS result. " +
            $"bottleId={bottleId}, frame_id={frameId}, " +
            $"ros=({rosPosition.x:F4}, {rosPosition.y:F4}, {rosPosition.z:F4}), " +
            $"unityLocal={unityLocalPosition}, unityWorld={unityWorldPosition}");
    }
}
