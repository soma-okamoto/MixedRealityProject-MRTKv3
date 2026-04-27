using UnityEngine;
using Meta.XR.MRUtilityKit;

public class QRCodeTrackerAndAnchorUpdater : MonoBehaviour
{

    [SerializeField] private GameObject objectPrefab;
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.005f;
    [SerializeField] private float lineOffset = 0.001f;

    private MRUKTrackable currentTrackable;
    private GameObject previewObject;
    private LineRenderer previewBorder;

    public bool IsAnchorReady { get; private set; } = false;

    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        if (SharedAnchorManager.Instance == null)
        {
            Debug.LogError("SharedAnchorManager が見つかりません");
            return;
        }

        currentTrackable = trackable;
        IsAnchorReady = true;

        SharedAnchorManager.Instance.ForceSetAnchor(
            trackable.transform.position,
            trackable.transform.rotation
        );

        if (objectPrefab != null && previewObject == null)
        {
            previewObject = Instantiate(objectPrefab);
        }

        if (previewBorder == null)
        {
            previewBorder = CreateBorderObject(trackable);
        }

        UpdatePreview();
        LogQRSize(trackable);
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (trackable != currentTrackable)
            return;

        currentTrackable = null;
        Debug.Log("現在追跡中のQRを見失いました");
    }

    private void LateUpdate()
    {
        if (currentTrackable == null || SharedAnchorManager.Instance == null)
            return;

        SharedAnchorManager.Instance.ForceSetAnchor(
            currentTrackable.transform.position,
            currentTrackable.transform.rotation
        );

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        Transform anchor = SharedAnchorManager.Instance.SharedAnchorRoot;

        if (previewObject != null)
        {
            Vector3 worldPos = anchor.TransformPoint(localOffset);
            Quaternion worldRot = anchor.rotation * Quaternion.Euler(localEulerOffset);

            previewObject.transform.position = worldPos;
            previewObject.transform.rotation = worldRot;
        }

        if (previewBorder != null && currentTrackable != null)
        {
            UpdateBorder(currentTrackable, previewBorder);
        }
    }

    private LineRenderer CreateBorderObject(MRUKTrackable trackable)
    {
        GameObject borderObj = new GameObject("QR_Border");
        borderObj.transform.SetParent(trackable.transform, false);
        borderObj.transform.localPosition = Vector3.zero;
        borderObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = borderObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = lineWidth;

        if (lineMaterial != null)
        {
            lr.material = lineMaterial;
        }

        UpdateBorder(trackable, lr);
        return lr;
    }

    private void UpdateBorder(MRUKTrackable trackable, LineRenderer lr)
    {
        if (trackable.PlaneBoundary2D == null || trackable.PlaneBoundary2D.Count < 2)
            return;

        int count = trackable.PlaneBoundary2D.Count;
        lr.positionCount = count;

        Vector3[] positions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Vector2 p = trackable.PlaneBoundary2D[i];
            positions[i] = new Vector3(p.x, p.y, lineOffset);
        }

        lr.SetPositions(positions);
    }

    private void LogQRSize(MRUKTrackable trackable)
    {
        if (!trackable.PlaneRect.HasValue)
        {
            Debug.Log("PlaneRect がまだ取得できていません");
            return;
        }

        Rect r = trackable.PlaneRect.Value;
        float width = r.width;
        float height = r.height;
        Debug.Log($"QR size: width={width:F4}m, height={height:F4}m");
    }
    public void DebugSetAnchorAsQRCodeRead()
{
    if (SharedAnchorManager.Instance == null)
    {
        Debug.LogError("SharedAnchorManager が見つかりません");
        return;
    }

    Vector3 debugAnchorPosition = transform.position;
    Quaternion debugAnchorRotation = transform.rotation;

    SharedAnchorManager.Instance.ForceSetAnchor(
        debugAnchorPosition,
        debugAnchorRotation
    );

    IsAnchorReady = true;

    Debug.Log($"Debug QR anchor set. pos={debugAnchorPosition}, rot={debugAnchorRotation.eulerAngles}");
}

}