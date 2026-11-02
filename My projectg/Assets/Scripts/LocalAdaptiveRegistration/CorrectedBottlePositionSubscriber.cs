using System.Collections.Generic;
using RosSharp.RosBridgeClient;
using place_estimation = RosSharp.RosBridgeClient.MessageTypes.PlaceEstimation;
using UnityEngine;

/// <summary>
/// Receives the final bottle position calculated by ROS.
/// Registered bottle IDs select the target; results are applied on the main thread.
/// </summary>
public class CorrectedBottlePositionSubscriber : UnitySubscriber<place_estimation.BottleDistribution>
{
    private const string ResultTopic = "/P_place_with_id";

    [Header("Coordinate conversion")]
    [Tooltip("Unity ObjectList used as the parent/reference for bottle coordinates.")]
    [SerializeField]
    private Transform defaultObjectList;

    private readonly object messageLock = new object();
    private readonly Queue<ReceivedPosition> receivedPositions = new Queue<ReceivedPosition>();
    // This dictionary and all Unity object references are accessed only on the main thread.
    private readonly Dictionary<int, BottleTarget> registeredBottles = new Dictionary<int, BottleTarget>();

    private sealed class BottleTarget
    {
        public GameObject Bottle;
        public Transform ObjectList;
    }

    private struct ReceivedPosition
    {
        public int BottleId;
        public float X;
        public float Y;
        public float Z;
        public string FrameId;
        public bool HasPosition;
    }

    protected override void Start()
    {
        // Migrate saved components which still point at the old, ID-less result.
        if (string.IsNullOrWhiteSpace(Topic) || Topic == "/P_place")
            Topic = ResultTopic;

        base.Start();
        Debug.Log($"[CorrectedBottlePosition] Subscriber started. topic={Topic}");
    }

    /// <summary>
    /// Registers the same bottle ID that is sent in /P_current.data[0].
    /// Call on the main thread before publishing P_current and PLACE.
    /// </summary>
    public bool ArmForBottle(int bottleId, GameObject bottle, Transform objectListTransform = null)
    {
        if (bottleId < 0)
        {
            Debug.LogWarning($"[CorrectedBottlePosition] Cannot register uninitialized bottleId={bottleId}.");
            return false;
        }

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

        if (registeredBottles.TryGetValue(bottleId, out var existing) &&
            !ReferenceEquals(existing.Bottle, bottle))
        {
            Debug.LogError(
                $"[CorrectedBottlePosition] Duplicate bottleId={bottleId}. " +
                $"Another bottle is already registered; {bottle.name} was not registered.");
            return false;
        }

        registeredBottles[bottleId] = new BottleTarget { Bottle = bottle, ObjectList = objectList };

        Debug.Log(
            $"[CorrectedBottlePosition] Registered ROS result target. " +
            $"bottleId={bottleId}, bottle={bottle.name}, objectList={objectList.name}");
        return true;
    }

    /// <summary>Clears all registered targets and queued results on the main thread.</summary>
    public void CancelPendingResult()
    {
        registeredBottles.Clear();
        lock (messageLock)
        {
            receivedPositions.Clear();
        }
    }

    protected override void ReceiveMessage(place_estimation.BottleDistribution message)
    {
        if (message == null)
            return;

        // Copy message data only. ROS callbacks must not access Unity objects.
        var received = new ReceivedPosition
        {
            BottleId = message.bottle_id,
            X = message.position != null ? (float)message.position.x : 0f,
            Y = message.position != null ? (float)message.position.y : 0f,
            Z = message.position != null ? (float)message.position.z : 0f,
            FrameId = message.header != null ? message.header.frame_id : string.Empty,
            HasPosition = message.position != null
        };

        lock (messageLock)
        {
            receivedPositions.Enqueue(received);
        }
    }

    private void Update()
    {
        int count;
        lock (messageLock)
            count = receivedPositions.Count;

        // Process this frame's queue in order, without overwriting another ID's result.
        for (var i = 0; i < count; i++)
        {
            ReceivedPosition received;
            lock (messageLock)
            {
                if (receivedPositions.Count == 0)
                    break;
                received = receivedPositions.Dequeue();
            }

            ApplyPosition(received);
        }
    }

    private void ApplyPosition(ReceivedPosition received)
    {
        var bottleId = received.BottleId;
        if (bottleId < 0 || !registeredBottles.TryGetValue(bottleId, out var target))
        {
            Debug.LogWarning($"[CorrectedBottlePosition] Ignored unregistered bottleId={bottleId}.");
            return;
        }

        if (!received.HasPosition ||
            float.IsNaN(received.X) || float.IsInfinity(received.X) ||
            float.IsNaN(received.Y) || float.IsInfinity(received.Y) ||
            float.IsNaN(received.Z) || float.IsInfinity(received.Z))
        {
            Debug.LogWarning($"[CorrectedBottlePosition] Ignored invalid position for bottleId={bottleId}.");
            return;
        }

        var bottle = target.Bottle;
        var objectList = target.ObjectList;
        var rosPosition = new Vector3(received.X, received.Y, received.Z);
        var frameId = received.FrameId;

        if (bottle == null || objectList == null)
        {
            Debug.LogWarning(
                "[CorrectedBottlePosition] ROS result received, but the target " +
                $"bottle or ObjectList no longer exists. bottleId={bottleId}");
            return;
        }

        // Existing conversion used by P_current and YouBotPosSubscriber:
        // ROS robot (x, y, z) -> Unity local (-x, -z, y).
        var unityLocalPosition = new Vector3(
            -rosPosition.x,
            -rosPosition.z,
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
