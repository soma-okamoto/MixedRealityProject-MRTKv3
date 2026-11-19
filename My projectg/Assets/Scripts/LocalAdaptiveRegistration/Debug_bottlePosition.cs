using UnityEngine;
using UnityEngine.Serialization;

public class Debug_bottlePosition : MonoBehaviour
{
    [Tooltip("3つの位置デバッグSubscriberを配置したGameObjectを指定します。")]
    [FormerlySerializedAs("rosConnector")]
    [SerializeField] private GameObject rosConnector2;

    public void ONPub()
    {
        SetSubscribersEnabled(true);
    }

    public void OFFPub()
    {
        SetSubscribersEnabled(false);
    }

    private void SetSubscribersEnabled(bool value)
    {
        if (rosConnector2 == null)
        {
            Debug.LogError(
                "[Debug_bottlePosition] Ros Connector 2が未設定です。" +
                "位置デバッグSubscriberのあるGameObjectを指定してください。", this);
            return;
        }

        // Resolve and change each component independently so one missing reference
        // cannot prevent the other two components from being switched.
        var place = rosConnector2.GetComponent<P_placePositionDebugSubscriber>();
        var yolo = rosConnector2.GetComponent<P_yoloPositionDebugSubscriber>();
        var meta = rosConnector2.GetComponent<P_metaPositionDebugSubscriber>();

        // Finish every change before logging; log callbacks may update scene UI.
        if (place != null) place.enabled = value;
        if (yolo != null) yolo.enabled = value;
        if (meta != null) meta.enabled = value;

        ReportSubscriberState(place);
        ReportSubscriberState(yolo);
        ReportSubscriberState(meta);

        if (value && !rosConnector2.activeInHierarchy)
        {
            Debug.LogWarning(
                $"[Debug_bottlePosition] {rosConnector2.name}または親GameObjectが非アクティブです。" +
                "SubscriberのチェックがONでも、表示更新は動きません。", rosConnector2);
        }
    }

    private void ReportSubscriberState<T>(T subscriber) where T : Behaviour
    {
        if (subscriber == null)
        {
            Debug.LogError(
                $"[Debug_bottlePosition] {rosConnector2.name}に{typeof(T).Name}がありません。", rosConnector2);
            return;
        }

        Debug.Log(
            $"[Debug_bottlePosition] target={rosConnector2.name}, component={typeof(T).Name}, " +
            $"enabled={subscriber.enabled}, active={subscriber.isActiveAndEnabled}", subscriber);
    }
}
