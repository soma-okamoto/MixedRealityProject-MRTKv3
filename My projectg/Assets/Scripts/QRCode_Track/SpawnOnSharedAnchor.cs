using UnityEngine;

public class SpawnOnSharedAnchor : MonoBehaviour
{
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    private void Start()
    {
        if (SharedAnchorManager.Instance == null)
        {
            Debug.LogError("SharedAnchorManager が見つかりません");
            return;
        }

        if (!SharedAnchorManager.Instance.IsInitialized)
        {
            Debug.LogWarning("共通アンカーがまだ初期化されていません");
            return;
        }

        Transform anchor = SharedAnchorManager.Instance.SharedAnchorRoot;

        Vector3 worldPos = anchor.TransformPoint(localOffset);
        Quaternion worldRot = anchor.rotation * Quaternion.Euler(localEulerOffset);

        Instantiate(targetPrefab, worldPos, worldRot);
    }
}