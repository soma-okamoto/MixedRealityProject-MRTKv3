using UnityEngine;
using Meta.XR.MRUtilityKit;

public class QRCodeAnchorInitializer : MonoBehaviour
{
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    private GameObject previewObject;

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

        if (!SharedAnchorManager.Instance.IsInitialized)
        {
            SharedAnchorManager.Instance.InitializeAnchor(
                trackable.transform.position,
                trackable.transform.rotation
            );

            Debug.Log("QR位置を保存しました");
        }

        if (previewPrefab != null && previewObject == null)
        {
            Transform anchor = SharedAnchorManager.Instance.SharedAnchorRoot;

            Vector3 worldPos = anchor.TransformPoint(localOffset);
            Quaternion worldRot = anchor.rotation * Quaternion.Euler(localEulerOffset);

            previewObject = Instantiate(previewPrefab, worldPos, worldRot);
        }

        IsAnchorReady = true;
    }
}