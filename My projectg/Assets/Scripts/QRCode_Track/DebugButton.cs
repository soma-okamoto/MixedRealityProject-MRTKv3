using UnityEngine;

public class DebugButton : MonoBehaviour
{
    [Header("QR Debug")]
    [SerializeField] private QRCodeTrackerAndAnchorUpdater qrTracker;

    [Header("Target Script")]
    [SerializeField] private TrackSceneButtonAction targetScript;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (qrTracker == null)
            {
                Debug.LogWarning("QR Tracker が設定されていません");
                return;
            }

            if (targetScript == null)
            {
                Debug.LogWarning("Target Script が設定されていません");
                return;
            }

            // QRコードを読み取れたことにする
            qrTracker.DebugSetAnchorAsQRCodeRead();

            // MainSceneへ遷移
            targetScript.GoToMainScene();
        }
    }
}