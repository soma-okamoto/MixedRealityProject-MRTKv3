using UnityEngine;
using RosSharp.RosBridgeClient;

public class MoveExistingObjectOnSharedAnchor : MonoBehaviour
{
    [Header("Move Target")]
    [SerializeField] private Transform targetObject;

    [Header("ROS Subscriber")]
    [SerializeField] private QRPositionSubscriber qrPositionSubscriber;

    [Header("Offset from ROS position")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    [Header("Settings")]
    [SerializeField] private bool useRosRotation = false;

    private Transform anchor;
    private bool hasMovedOnce = false;

    private void Update()
    {
        if (hasMovedOnce)
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
        Vector3 localPos = qrPositionSubscriber.messageUnityPosition + localOffset;
        Vector3 worldPos = anchor.TransformPoint(localPos);

        Quaternion worldRot;

        if (useRosRotation)
        {
            worldRot = anchor.rotation * qrPositionSubscriber.messageUnityRotation * Quaternion.Euler(localEulerOffset);
        }
        else
        {
            worldRot = anchor.rotation * Quaternion.Euler(localEulerOffset);
        }

        targetObject.SetPositionAndRotation(worldPos, worldRot);

        Debug.Log($"Target moved once to {worldPos}");
    }
}