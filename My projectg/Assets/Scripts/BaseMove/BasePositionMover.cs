using System.Diagnostics;
using UnityEngine;

/// <summary>
/// YoubotOffsetSubscriber の BaseMovePosition を使って
/// 指定したオブジェクトを初期配置からの相対移動として動かすコンポーネント
/// </summary>
public class BasePositionMover : MonoBehaviour
{
    [Tooltip("BaseMovePosition を計算している Subscriber")]
    [SerializeField] private YoubotOffsetSubscriber offsetSubscriber;

    [Tooltip("移動対象の Transform")]
    [SerializeField] private Transform targetTransform;

    // シーン上の最初のローカル位置を記憶
    private Vector3 initialLocalPos;

    void Start()
    {
        if (offsetSubscriber == null || targetTransform == null)
        {
            UnityEngine.Debug.LogError("OffsetSubscriber または TargetTransform が設定されていません。");
            enabled = false;
            return;
        }
        // 最初のローカル位置をキャプチャ
        initialLocalPos = targetTransform.localPosition;
    }

    void Update()
    {
        // Subscriber で計算された BaseMovePosition は
        // 「原点(0,0,0) からの絶対位置」ではなく「最初のオフセットからの相対位置」
        // になっている想定です。もし絶対座標なら position を使ってください。

        // ここでは initialLocalPos に相対オフセットを足してやります。
        Vector3 relativeOffset = offsetSubscriber.BaseMovePosition;
        targetTransform.localPosition = initialLocalPos + relativeOffset;
    }
}
