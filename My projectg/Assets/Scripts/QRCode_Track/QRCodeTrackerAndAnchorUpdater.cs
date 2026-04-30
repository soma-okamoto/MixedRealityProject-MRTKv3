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

    [SerializeField] private float axisLength = 0.15f;
    [SerializeField] private float axisWidth = 0.01f;

    [SerializeField] private float axisZOffset = 0.02f;

    [SerializeField] private Material xAxisMaterial;
    [SerializeField] private Material yAxisMaterial;
    [SerializeField] private Material zAxisMaterial;
    

    private LineRenderer xAxis;
    private LineRenderer yAxis;
    private LineRenderer zAxis;

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
        UpdateAxis();
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

    private LineRenderer CreateAxisObject(MRUKTrackable trackable, string name, Material material)
    {
        GameObject axisObj = new GameObject(name);
        axisObj.transform.SetParent(trackable.transform, false);
        axisObj.transform.localPosition = Vector3.zero;
        axisObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = axisObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.loop = false;
        lr.widthMultiplier = axisWidth;

        if (material != null)
        {
            lr.material = material;
        }
        else if (lineMaterial != null)
        {
            lr.material = lineMaterial;
        }

        return lr;
    }

    private Vector2 GetQRLocalCenter(MRUKTrackable trackable)
    {
        if (trackable.PlaneBoundary2D == null || trackable.PlaneBoundary2D.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;

        foreach (Vector2 p in trackable.PlaneBoundary2D)
        {
            sum += p;
        }

        return sum / trackable.PlaneBoundary2D.Count;
    }

    private void UpdateAxis()
    {
        if (currentTrackable == null)
            return;

        if (xAxis == null)
            xAxis = CreateAxisObject(currentTrackable, "QR_X_Axis", xAxisMaterial);

        if (yAxis == null)
            yAxis = CreateAxisObject(currentTrackable, "QR_Y_Axis", yAxisMaterial);

        if (zAxis == null)
            zAxis = CreateAxisObject(currentTrackable, "QR_Z_Axis", zAxisMaterial);

        Vector2 center2D = GetQRLocalCenter(currentTrackable);

        Vector3 origin = new Vector3(
            center2D.x,
            center2D.y,
            axisZOffset
        );

        xAxis.SetPosition(0, origin);
        xAxis.SetPosition(1, origin + new Vector3(axisLength, 0f, 0f));

        yAxis.SetPosition(0, origin);
        yAxis.SetPosition(1, origin + new Vector3(0f, axisLength, 0f));

        zAxis.SetPosition(0, origin);
        zAxis.SetPosition(1, origin + new Vector3(0f, 0f, axisLength));
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