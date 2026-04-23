using UnityEngine;

public class FollowUserUI : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private float distance = 0.45f;
    [SerializeField] private float heightOffset = -0.05f;
    [SerializeField] private float X_Offset = -0.02f;
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private bool yawOnly = true;

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 forward = targetCamera.forward;

        if (yawOnly)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();
        }

        Vector3 targetPos = targetCamera.position + forward * distance;
        targetPos.y += heightOffset;
        targetPos.x += X_Offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * followSpeed
        );

        Vector3 lookDir = transform.position - targetCamera.position;

        if (yawOnly)
        {
            lookDir.y = 0f;
        }

        if (lookDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }
    }
}