using UnityEngine;
using UnityEngine.InputSystem; // ← 新しいInput Systemを使うために追加

public class DebugButton : MonoBehaviour
{
    [Header("QR Debug")]
    [SerializeField] private QRCodeTrackerAndAnchorUpdater qrTracker;

    [Header("Target Script")]
    [SerializeField] private TrackSceneButtonAction targetScript;

    private void Update()
    {
        // Keyboardが接続されているか確認し、Gキーが「押された瞬間」を検知
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
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