using UnityEngine;
using RosSharp.RosBridgeClient;
using System.Diagnostics;

public class YoubotOffsetToggler : MonoBehaviour
{
    [Tooltip("トグルしたい YoubotOffsetSubscriber コンポーネント")]
    [SerializeField] private YoubotOffsetSubscriber subscriber;

    /// <summary>
    /// ボタンの OnClicked へアタッチ
    /// </summary>
    public void ToggleSubscriber()
    {
        if (subscriber == null)
        {
            UnityEngine.Debug.LogWarning("subscriber がアサインされていません。");
            return;
        }
        subscriber.enabled = !subscriber.enabled;
        UnityEngine.Debug.Log($"YoubotOffsetSubscriber is now {(subscriber.enabled ? "Enabled" : "Disabled")}");
    }
}
