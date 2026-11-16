using System.Collections.Generic;
using TMPro;
using UnityEngine;
using place_estimation = RosSharp.RosBridgeClient.MessageTypes.PlaceEstimation;

/// <summary>
/// Visualizes the latest /P_place_with_id result for each bottle ID.
/// Only bottle_id and position.xyz are used; covariance is intentionally ignored.
/// </summary>
public class P_placePositionDebugSubscriber : RosSharp.RosBridgeClient.UnitySubscriber<place_estimation.BottleDistribution>
{
    [Header("Visualization")]
    [Tooltip("Base transform for the ROS coordinates.")]
    public GameObject OperationOrigin;

    [Tooltip("Debug marker prefab. Its first TMP text is updated with the bottle ID.")]
    public GameObject TestObject;

    [SerializeField]
    private bool logPositions = true;

    [SerializeField]
    private bool showBottleId = true;

    public int LastBottleId { get; private set; } = -1;
    public Vector3 LastRosPosition { get; private set; }
    public Vector3 LastUnityPosition { get; private set; }

    private struct ReceivedPosition
    {
        public int BottleId;
        public float X;
        public float Y;
        public float Z;
        public string FrameId;
    }

    private sealed class DebugMarker
    {
        public GameObject Object;
        public TMP_Text Label;
    }

    private readonly object messageLock = new object();
    private readonly Dictionary<int, ReceivedPosition> pendingPositions =
        new Dictionary<int, ReceivedPosition>();
    private readonly Dictionary<int, DebugMarker> markers =
        new Dictionary<int, DebugMarker>();

    protected override void Start()
    {
        if (string.IsNullOrWhiteSpace(Topic))
            Topic = "/P_place_with_id";

        base.Start();
        Debug.Log($"[P_placePositionDebug] Subscriber started. topic={Topic}");
    }

    protected override void ReceiveMessage(place_estimation.BottleDistribution message)
    {
        if (message == null || message.position == null)
            return;

        // Copy only message data here. Unity objects are accessed in Update().
        var received = new ReceivedPosition
        {
            BottleId = message.bottle_id,
            X = (float)message.position.x,
            Y = (float)message.position.y,
            Z = (float)message.position.z,
            FrameId = message.header != null ? message.header.frame_id : string.Empty
        };

        lock (messageLock)
        {
            // Keep the newest result for each ID if ROS publishes faster than Unity renders.
            pendingPositions[received.BottleId] = received;
        }
    }

    private void Update()
    {
        List<ReceivedPosition> positions;
        lock (messageLock)
        {
            if (pendingPositions.Count == 0)
                return;

            positions = new List<ReceivedPosition>(pendingPositions.Values);
            pendingPositions.Clear();
        }

        foreach (var received in positions)
            ApplyPosition(received);
    }

    private void ApplyPosition(ReceivedPosition received)
    {
        if (received.BottleId < 0)
        {
            Debug.LogWarning($"[P_placePositionDebug] Ignored invalid bottle_id={received.BottleId}.");
            return;
        }

        if (!IsFinite(received.X) || !IsFinite(received.Y) || !IsFinite(received.Z))
        {
            Debug.LogWarning(
                $"[P_placePositionDebug] Ignored non-finite position for bottle_id={received.BottleId}.");
            return;
        }

        if (OperationOrigin == null || TestObject == null)
        {
            Debug.LogWarning(
                "[P_placePositionDebug] Assign OperationOrigin and TestObject in the Inspector.");
            return;
        }

        var rosPosition = new Vector3(received.X, received.Y, received.Z);

        // Same ROS -> Unity mapping as P_metaPositionDebugSubscriber/P_yoloPositionDebugSubscriber.
        var unityLocalPosition = new Vector3(
            -rosPosition.x,
            rosPosition.z,
            -rosPosition.y);
        var unityWorldPosition = OperationOrigin.transform.TransformPoint(unityLocalPosition);

        var marker = GetOrCreateMarker(received.BottleId, OperationOrigin.transform);
        marker.Object.transform.position = unityWorldPosition;
        marker.Object.SetActive(true);

        if (showBottleId && marker.Label != null)
            marker.Label.text = $"P_place\nID: {received.BottleId}";

        LastBottleId = received.BottleId;
        LastRosPosition = rosPosition;
        LastUnityPosition = unityWorldPosition;

        if (logPositions)
        {
            Debug.Log(
                $"[P_placePositionDebug] id={received.BottleId}, frame_id={received.FrameId}, " +
                $"ROS=({rosPosition.x:F4}, {rosPosition.y:F4}, {rosPosition.z:F4}) m, " +
                $"UnityLocal={unityLocalPosition}, UnityWorld={unityWorldPosition}");
        }
    }

    private DebugMarker GetOrCreateMarker(int bottleId, Transform origin)
    {
        if (markers.TryGetValue(bottleId, out var existing) && existing.Object != null)
        {
            if (existing.Object.transform.parent != origin)
                existing.Object.transform.SetParent(origin, false);
            return existing;
        }

        var markerObject = Instantiate(TestObject, origin);
        markerObject.name = $"P_place_Debug_ID_{bottleId}";

        var marker = new DebugMarker
        {
            Object = markerObject,
            Label = markerObject.GetComponentInChildren<TMP_Text>(true)
        };
        markers[bottleId] = marker;
        return marker;
    }

    /// <summary>Hides all generated markers and clears queued ROS results.</summary>
    public void ClearMarkers()
    {
        lock (messageLock)
        {
            pendingPositions.Clear();
        }

        foreach (var marker in markers.Values)
        {
            if (marker.Object != null)
                marker.Object.SetActive(false);
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void OnDisable()
    {
        foreach (var marker in markers.Values)
        {
            if (marker.Object != null)
                marker.Object.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        foreach (var marker in markers.Values)
        {
            if (marker.Object != null)
                Destroy(marker.Object);
        }
        markers.Clear();
    }
}
