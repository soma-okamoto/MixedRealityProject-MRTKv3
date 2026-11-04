using RosSharp.RosBridgeClient.MessageTypes.Std;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visualizes /P_meta XYZ + covariance records relative to OperationOrigin.
/// Marker indices are detection order, not bottle IDs.
/// </summary>
public class P_metaPositionDebugSubscriber : RosSharp.RosBridgeClient.UnitySubscriber<RosSharp.RosBridgeClient.MessageTypes.Std.Float32MultiArray>
{
    private const int RecordSize = 12;

    [Header("Visualization")]
    [Tooltip("Base for visualization. Positions use this object's local axes, rotation and scale.")]
    public GameObject OperationOrigin;

    [Tooltip("Debug marker prefab. Use a simple sphere without bottle-operation components.")]
    public GameObject TestObject;

    [SerializeField]
    private bool logPositions = true;

    [Header("Latest XYZ only (covariance excluded)")]
    [Tooltip("ROS coordinates: [x0,y0,z0,x1,y1,z1,...]. These values have not been converted to Unity coordinates.")]
    public float[] messageData = new float[0];

    public int CandidateCount => messageData.Length / 3;

    private readonly object messageLock = new object();
    private readonly List<GameObject> markers = new List<GameObject>();
    private float[] pendingData;
    private bool hasPendingMessage;

    protected override void Start()
    {
        if (string.IsNullOrWhiteSpace(Topic))
            Topic = "/P_meta";

        base.Start();
        Debug.Log($"[P_metaDebug] Subscriber started. topic={Topic}");
    }

    protected override void ReceiveMessage(Float32MultiArray message)
    {
        // Keep the latest snapshot only; Unity APIs are used exclusively in Update.
        var snapshot = message != null && message.data != null
            ? (float[])message.data.Clone()
            : null;
        lock (messageLock)
        {
            pendingData = snapshot;
            hasPendingMessage = true;
        }
    }

    private void Update()
    {
        float[] data;
        lock (messageLock)
        {
            if (!hasPendingMessage)
                return;

            data = pendingData;
            pendingData = null;
            hasPendingMessage = false;
        }

        if (data == null || data.Length % RecordSize != 0)
        {
            Debug.LogWarning(
                $"[P_metaDebug] Ignored invalid /P_meta payload. " +
                $"Expected 12 values per candidate, received {(data == null ? "null" : data.Length.ToString())}.");
            return;
        }

        var positions = new List<Vector3>();
        for (var offset = 0; offset < data.Length; offset += RecordSize)
        {
            var x = data[offset];
            var y = data[offset + 1];
            var z = data[offset + 2];
            if (float.IsNaN(x) || float.IsInfinity(x) ||
                float.IsNaN(y) || float.IsInfinity(y) ||
                float.IsNaN(z) || float.IsInfinity(z))
            {
                Debug.LogWarning($"[P_metaDebug] Ignored non-finite XYZ in candidate {offset / RecordSize}.");
                continue;
            }

            positions.Add(new Vector3(x, y, z));
        }

        messageData = new float[positions.Count * 3];
        for (var i = 0; i < positions.Count; i++)
        {
            messageData[i * 3] = positions[i].x;
            messageData[i * 3 + 1] = positions[i].y;
            messageData[i * 3 + 2] = positions[i].z;
        }

        if (positions.Count == 0)
        {
            HideMarkersFrom(0);
            if (logPositions)
                Debug.Log("[P_metaDebug] No valid bottle candidates; markers hidden.");
            return;
        }

        if (OperationOrigin == null || TestObject == null)
        {
            HideMarkersFrom(0);
            Debug.LogWarning("[P_metaDebug] Assign OperationOrigin and the TestObject marker prefab.");
            return;
        }

        var origin = OperationOrigin.transform;
        for (var i = 0; i < positions.Count; i++)
        {
            var marker = GetOrCreateMarker(i, origin);
            var rosPosition = positions[i];

            // Preserve the Robot -> Unity mapping specified in the original debug script.
            var unityLocalPosition = new Vector3(-rosPosition.x, rosPosition.z, -rosPosition.y);
            var unityWorldPosition = origin.TransformPoint(unityLocalPosition);
            marker.transform.position = unityWorldPosition;
            marker.SetActive(true);

            if (logPositions)
            {
                Debug.Log(
                    $"[P_metaDebug] candidate={i}, origin={OperationOrigin.name}, " +
                    $"ROS=({rosPosition.x:F4}, {rosPosition.y:F4}, {rosPosition.z:F4}) m, " +
                    $"UnityLocal={unityLocalPosition}, UnityWorld={unityWorldPosition}");
            }
        }

        HideMarkersFrom(positions.Count);
    }

    private GameObject GetOrCreateMarker(int index, Transform origin)
    {
        if (index == markers.Count)
            markers.Add(null);

        if (markers[index] == null)
        {
            markers[index] = Instantiate(TestObject, origin);
            markers[index].name = $"P_meta_Debug_{index}";
        }
        else if (markers[index].transform.parent != origin)
        {
            markers[index].transform.SetParent(origin, false);
        }

        return markers[index];
    }

    private void HideMarkersFrom(int firstIndex)
    {
        for (var i = firstIndex; i < markers.Count; i++)
        {
            if (markers[i] != null)
                markers[i].SetActive(false);
        }
    }

    private void OnDisable()
    {
        HideMarkersFrom(0);
    }

    private void OnDestroy()
    {
        foreach (var marker in markers)
        {
            if (marker != null)
                Destroy(marker);
        }
    }
}
