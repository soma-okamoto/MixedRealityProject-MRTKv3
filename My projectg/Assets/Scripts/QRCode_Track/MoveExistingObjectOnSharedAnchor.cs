using UnityEngine;
using RosSharp.RosBridgeClient;

public class MoveExistingObjectOnSharedAnchor : MonoBehaviour
{
    [Header("Move Target")]
    [SerializeField] private Transform targetObject;

    [Header("ROS Subscriber")]
    [SerializeField] private QRPositionSubscriber qrPositionSubscriber;

    [Header("Offset in MRUK QR local coordinates")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    [Header("Settings")]
    [SerializeField] private bool useRosRotation = true;
    [SerializeField] private bool moveOnlyOnce = false;

    private Transform anchor;
    private bool hasMovedOnce = false;

    private void Update()
    {
        if (moveOnlyOnce && hasMovedOnce)
            return;

        if (!TrySetupAnchor())
            return;

        if (targetObject == null)
            return;

        if (qrPositionSubscriber == null)
            return;

        if (!qrPositionSubscriber.IsMessageReceived)
            return;

        MoveTargetObject();

        hasMovedOnce = true;
    }

    private bool TrySetupAnchor()
    {
        if (anchor != null)
            return true;

        if (SharedAnchorManager.Instance == null)
            return false;

        if (!SharedAnchorManager.Instance.IsInitialized)
            return false;

        anchor = SharedAnchorManager.Instance.SharedAnchorRoot;

        return anchor != null;
    }

    private void MoveTargetObject()
    {
        // messageUnityPosition は「MRUK QRローカル座標系でのBase位置」である前提
        Vector3 localPos = qrPositionSubscriber.messageUnityPosition + localOffset;

        // QRローカル → Unity World
        Vector3 worldPos = anchor.TransformPoint(localPos);

        Quaternion worldRot;

        if (useRosRotation)
        {
            // QR World回転 × QRローカル内のBase姿勢 × モデル補正
            worldRot =
                anchor.rotation
                * qrPositionSubscriber.messageUnityRotation
                * Quaternion.Euler(localEulerOffset);
        }
        else
        {
            // 位置確認だけしたい場合
            worldRot =
                anchor.rotation
                * Quaternion.Euler(localEulerOffset);
        }

        targetObject.SetPositionAndRotation(worldPos, worldRot);

        Debug.Log(
            $"Target moved. " +
            $"localPos={localPos}, worldPos={worldPos}, " +
            $"anchorRot={anchor.rotation.eulerAngles}, " +
            $"rosRot={qrPositionSubscriber.messageUnityRotation.eulerAngles}, " +
            $"worldRot={worldRot.eulerAngles}"
        );
    }
}