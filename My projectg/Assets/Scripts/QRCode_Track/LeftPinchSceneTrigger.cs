using UnityEngine;

public class LeftPinchSceneTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private TrackSceneButtonAction sceneAction;

    [Header("Settings")]
    [SerializeField] private float holdTime = 0.8f;

    private float pinchTimer = 0f;
    private bool alreadyTriggered = false;

    private void Update()
    {
        if (alreadyTriggered) return;

        if (leftHand == null || sceneAction == null)
        {
            return;
        }

        bool isLeftPinching =
            leftHand.IsDataValid &&
            leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        if (isLeftPinching)
        {
            pinchTimer += Time.deltaTime;

            if (pinchTimer >= holdTime)
            {
                alreadyTriggered = true;
                Debug.Log("Left Hand Pinch Detected: Executing GoToMainScene");
                sceneAction.GoToMainScene();
            }
        }
        else
        {
            pinchTimer = 0f;
        }
    }
}