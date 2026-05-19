using UnityEngine;
using System.Collections.Generic;

public class SpawnOnSharedAnchor : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private GameObject targetPrefabSub;

    [Header("Main Offset")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    [Header("Sub Offset")]
    [SerializeField] private Vector3 localOffsetSub = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffsetSub = Vector3.zero;

    private void Start()
    {
        if (SharedAnchorManager.Instance == null)
        {
            Debug.LogError("SharedAnchorManager が見つかりません");
            return;
        }

        SpawnMainObject();
        SpawnSubObjects();
    }

    private void SpawnMainObject()
    {
        if (!SharedAnchorManager.Instance.IsInitialized)
        {
            Debug.LogWarning("Mainアンカーがまだ初期化されていません");
            return;
        }

        if (targetPrefab == null)
        {
            Debug.LogError("targetPrefab が設定されていません");
            return;
        }

        Transform anchor = SharedAnchorManager.Instance.SharedAnchorRoot;

        Vector3 worldPos = anchor.TransformPoint(localOffset);
        Quaternion worldRot = anchor.rotation * Quaternion.Euler(localEulerOffset);

        GameObject go = Instantiate(targetPrefab, worldPos, worldRot);
        go.name = "Main_Object_From_SharedAnchor";

        Debug.Log($"Main object spawned at {worldPos}");
    }

    private void SpawnSubObjects()
    {
        if (!SharedAnchorManager.Instance.HasAnySubAnchor)
        {
            Debug.LogWarning("Subアンカーが1つも初期化されていません");
            return;
        }

        if (targetPrefabSub == null)
        {
            Debug.LogError("targetPrefabSub が設定されていません");
            return;
        }

        foreach (KeyValuePair<string, Transform> pair in SharedAnchorManager.Instance.SubAnchorRoots)
        {
            string subId = pair.Key;
            Transform anchorSub = pair.Value;

            if (anchorSub == null)
                continue;

            Vector3 worldPosSub = anchorSub.TransformPoint(localOffsetSub);
            Quaternion worldRotSub = anchorSub.rotation * Quaternion.Euler(localEulerOffsetSub);

            GameObject goSub = Instantiate(targetPrefabSub, worldPosSub, worldRotSub);
            goSub.name = $"Sub_Object_From_SharedAnchor_{subId}";

            Debug.Log($"Sub object spawned: {subId}, pos={worldPosSub}");
        }
    }
}