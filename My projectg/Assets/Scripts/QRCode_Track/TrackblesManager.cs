using UnityEngine;
using Meta.XR.MRUtilityKit;
using static OVRAnchor;
using System.Collections.Generic;
using System.Text;
using System.Collections;

public class TrackblesManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject mainPrefab;
    [SerializeField] private GameObject subPrefab;

    private MRUKTrackable mainTrackable;
    private QRTracker mainTracker;
    private bool hasMain = false;

    // private MRUKTrackable subTrackable;
    // private QRTracker subTracker;
    // private bool hasSub = false;
    private readonly Dictionary<string, MRUKTrackable> subTrackables =
    new Dictionary<string, MRUKTrackable>();

    private readonly Dictionary<string, QRTracker> subTrackers =
    new Dictionary<string, QRTracker>();


    [Header("Spawn Settings")]
    [SerializeField] private int waitFramesBeforeSpawn = 10;

    private readonly Dictionary<MRUKTrackable, QRTracker> spawnedTrackers =
        new Dictionary<MRUKTrackable, QRTracker>();

    private string CleanQRId(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        StringBuilder sb = new StringBuilder();

        foreach (char c in raw)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }


    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        Debug.Log("Trackable added");

        if (trackable == null)
        {
            Debug.LogWarning("Trackable is null");
            return;
        }

        if (trackable.TrackableType != TrackableType.QRCode)
        {
            Debug.Log("Trackable is not a QR code");
            return;
        }

        if (spawnedTrackers.ContainsKey(trackable))
        {
            Debug.Log("This trackable already has a spawned object");
            return;
        }

         if (SharedAnchorManager.Instance == null)
        {
            Debug.LogError("SharedAnchorManager missing");
            return;
        }

        string rawId = CleanQRId(trackable.MarkerPayloadString);

        Debug.Log($"Cleaned QR code ID: '{rawId}'");

        if (rawId != "Main" && !IsSubId(rawId))
        {
            Debug.Log($"Unknown QR code ID: '{rawId}'");
            return;
        }

        StartCoroutine(SpawnAfterTrackableReady(trackable, rawId));
    }

   private IEnumerator SpawnAfterTrackableReady(MRUKTrackable trackable, string rawId)
{
    for (int i = 0; i < waitFramesBeforeSpawn; i++)
    {
        yield return null;
    }

    if (trackable == null)
        yield break;

    if (spawnedTrackers.ContainsKey(trackable))
    {
        Debug.Log("Spawn skipped: already spawned after waiting");
        yield break;
    }

    if (rawId == "Main")
    {
        SpawnMain(trackable, rawId);
    }
    else if (IsSubId(rawId))
    {
        SpawnSub(trackable, rawId);
    }
}

private bool IsSubId(string rawId)
{
    return !string.IsNullOrEmpty(rawId) && rawId.StartsWith("Sub");
}





    private void SpawnMain(MRUKTrackable trackable, string rawId)
    {
        if (hasMain)
        {
            Debug.LogWarning("Main QR is already registered. Ignore duplicated Main.");
            return;
        }

        if (mainPrefab == null)
        {
            Debug.LogError("mainPrefab is not assigned");
            return;
        }

        GameObject go = InstantiateOnQRCode(trackable, mainPrefab, "MainQRObject");

        QRTracker qrTracker = GetOrAddQRTracker(go);
        qrTracker.QRid = "Main";
        qrTracker.RawQRid = rawId;
        qrTracker.ApplyLabel();

        spawnedTrackers[trackable] = qrTracker;

        mainTrackable = trackable;
        mainTracker = qrTracker;

        hasMain = true;

        SharedAnchorManager.Instance.ForceSetAnchor(
            trackable.transform.position,
            trackable.transform.rotation
        );

        Debug.Log("Main QR code detected and spawned");
    }


    private void SpawnSub(MRUKTrackable trackable, string rawId)
    {
        if (subTrackers.ContainsKey(rawId))
        {
            Debug.LogWarning($"{rawId} QR is already registered. Ignore duplicated Sub.");
            return;
        }

        if (subPrefab == null)
        {
            Debug.LogError("subPrefab が設定されていません");
            return;
        }

        GameObject go = InstantiateOnQRCode(trackable, subPrefab, $"{rawId}_QRObject");

        QRTracker qrTracker = GetOrAddQRTracker(go);
        qrTracker.QRid = rawId;
        qrTracker.RawQRid = rawId;
        qrTracker.ApplyLabel();

        spawnedTrackers[trackable] = qrTracker;

        subTrackables[rawId] = trackable;
        subTrackers[rawId] = qrTracker;

        Vector3 anchorPos = GetQRWorldPosition(trackable);

        SharedAnchorManager.Instance.ForceSetSubAnchor(
            rawId,
            anchorPos,
            trackable.transform.rotation
        );

        Debug.Log($"{rawId} QR code detected and spawned");
    }


    private GameObject InstantiateOnQRCode(
        MRUKTrackable trackable,
        GameObject prefab,
        string objectName
    )
    {
        Vector3 worldPos = GetQRWorldPosition(trackable);
        Quaternion worldRot = trackable.transform.rotation;

        GameObject go = Instantiate(prefab, worldPos, worldRot);
        go.name = objectName;

        Debug.Log(
            $"InstantiateOnQRCode: {objectName}, " +
            $"trackablePos={trackable.transform.position}, " +
            $"qrWorldCenter={worldPos}, " +
            $"spawnPos={go.transform.position}, " +
            $"planeCount={(trackable.PlaneBoundary2D == null ? -1 : trackable.PlaneBoundary2D.Count)}"
        );

        return go;
    }

    private QRTracker GetOrAddQRTracker(GameObject go)
    {
        QRTracker qrTracker = go.GetComponent<QRTracker>();

        if (qrTracker == null)
        {
            qrTracker = go.AddComponent<QRTracker>();
        }

        return qrTracker;
    }



    private Vector3 GetQRWorldPosition(MRUKTrackable trackable)
    {
        if (trackable == null)
            return Vector3.zero;

        Vector3 localCenter = GetQRLocalCenter(trackable);

        return trackable.transform.TransformPoint(localCenter);
    }

    private Vector3 GetQRLocalCenter(MRUKTrackable trackable)
    {
        if (trackable == null)
            return Vector3.zero;

        if (trackable.PlaneBoundary2D == null || trackable.PlaneBoundary2D.Count == 0)
        {
            Debug.LogWarning("PlaneBoundary2D が取得できないため localCenter=zero を使います");
            return Vector3.zero;
        }

        Vector2 center = Vector2.zero;

        foreach (Vector2 p in trackable.PlaneBoundary2D)
        {
            center += p;
        }

        center /= trackable.PlaneBoundary2D.Count;

        return new Vector3(center.x, center.y, 0f);
    }








    private void Update()
    {
        UpdateMainObjectPose();
        UpdateSubObjectPose();
    }

    private void UpdateMainObjectPose()
    {
        if (mainTrackable == null || mainTracker == null)
            return;

        UpdateObjectPoseOnQRCode(mainTrackable, mainTracker.gameObject);

        if (SharedAnchorManager.Instance == null)
            return;

        SharedAnchorManager.Instance.ForceSetAnchor(
            mainTrackable.transform.position,
            mainTrackable.transform.rotation
        );
    }

    private void UpdateSubObjectPose()
    {
        if (SharedAnchorManager.Instance == null)
            return;

        foreach (KeyValuePair<string, MRUKTrackable> pair in subTrackables)
        {
            string subId = pair.Key;
            MRUKTrackable trackable = pair.Value;

            if (trackable == null)
                continue;

            if (!subTrackers.TryGetValue(subId, out QRTracker tracker))
                continue;

            if (tracker == null)
                continue;

            UpdateObjectPoseOnQRCode(trackable, tracker.gameObject);

            Vector3 anchorPos = GetQRWorldPosition(trackable);

            SharedAnchorManager.Instance.ForceSetSubAnchor(
                subId,
                anchorPos,
                trackable.transform.rotation
            );
        }
    }


    private void UpdateObjectPoseOnQRCode(MRUKTrackable trackable, GameObject obj)
    {
        if (trackable == null || obj == null)
            return;

        Vector3 worldPos = GetQRWorldPosition(trackable);
        Quaternion worldRot = trackable.transform.rotation;

        obj.transform.SetPositionAndRotation(worldPos, worldRot);
    }



    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (trackable == null)
            return;

        if (!spawnedTrackers.TryGetValue(trackable, out QRTracker tracker))
            return;

        if (tracker != null)
        {
            Debug.Log($"Trackable removed: {tracker.QRid}");

            if (tracker.QRid != "Main")
            {
                subTrackables.Remove(tracker.QRid);
                subTrackers.Remove(tracker.QRid);
            }
        }

        spawnedTrackers.Remove(trackable);
    }



}