using UnityEngine;
using Meta.XR.MRUtilityKit;

public class QRCodeAnchorInitializer : MonoBehaviour
{
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    private GameObject previewObject;
    private MRUKTrackable currentTrackable;

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

        // 初回でも再検出でも、その時点の最新 pose を反映
        SharedAnchorManager.Instance.ForceSetAnchor(
            trackable.transform.position,
            trackable.transform.rotation
        );

        Debug.Log("QR位置を更新しました");

        if (previewPrefab != null && previewObject == null)
        {
            previewObject = Instantiate(previewPrefab);
        }

        UpdatePreviewObject();
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
        if (currentTrackable == null)
            return;

        if (SharedAnchorManager.Instance == null)
            return;

        // 常に最新のQR poseを共有アンカーへ反映
        SharedAnchorManager.Instance.ForceSetAnchor(
            currentTrackable.transform.position,
            currentTrackable.transform.rotation
        );

        UpdatePreviewObject();
    }

    private void UpdatePreviewObject()
    {
        if (previewObject == null || SharedAnchorManager.Instance == null)
            return;

        Transform anchor = SharedAnchorManager.Instance.SharedAnchorRoot;

        Vector3 worldPos = anchor.TransformPoint(localOffset);
        Quaternion worldRot = anchor.rotation * Quaternion.Euler(localEulerOffset);

        previewObject.transform.position = worldPos;
        previewObject.transform.rotation = worldRot;
    }
}