using UnityEngine;
using UnityEngine.SceneManagement;

public class TrackSceneButtonAction : MonoBehaviour
{
    [SerializeField] private QRCodeAnchorInitializer qrInitializer;
    [SerializeField] private string nextSceneName = "main";

    public void GoToMainScene()
    {
        if (qrInitializer == null)
        {
            Debug.LogError("QRCodeAnchorInitializer が未設定です");
            return;
        }

        if (!qrInitializer.IsAnchorReady)
        {
            Debug.Log("まだQRが読み取れていません");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}