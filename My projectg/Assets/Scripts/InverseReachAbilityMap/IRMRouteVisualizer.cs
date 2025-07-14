using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit.SpatialManipulation;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Nav;
using static MixedReality.Toolkit.SpatialManipulation.ObjectManipulator;
using MixedReality.Toolkit.Input;
using RosSharp.Urdf;


public class IRMRouteVisual : MonoBehaviour
{
    [Header("References")]
    public IRMRouteSubscriber    subscriber;    // 経路購読
    public IRMRoutePathPublisher publisher;     // 経路再配信

    [Header("Prefabs & Transforms")]
    [Tooltip("編集可能ウェイポイント用プレハブ(ObjectManipulator付き)")]
    public GameObject waypointPrefab;
    [Tooltip("LineRenderer付きプレハブ")]
    public GameObject lineRendererPrefab;
    [Tooltip("経路のスタート地点にしたいオブジェクトのTransform")]
    public Transform startTransform;

    [Tooltip("初回表示用案内メッセージ(3D Textなど)")]
    public GameObject suggestionMessage;

    [Header("Display Settings")]
    [Tooltip("案内メッセージ表示位置(カメラ前の距離)")]
    public float messageDistance = 2.0f;

    private List<GameObject> waypoints     = new List<GameObject>();
    private LineRenderer     lineRenderer;
    private bool             hasVisualized = false;

    void Start()
    {
        subscriber = subscriber ?? GetComponent<IRMRouteSubscriber>();
        publisher  = publisher  ?? GetComponent<IRMRoutePathPublisher>();

        // LineRenderer 準備
        var lrObj = Instantiate(lineRendererPrefab, transform);
        lineRenderer = lrObj.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        // Publisher 初期化
        publisher.WayPointObjectList = new List<GameObject>();
    }

    void Update()
    {
        if (subscriber.messagePath != null && !hasVisualized)
        {
            Visualize(subscriber.messagePath);
            ShowSuggestionMessage();
            hasVisualized = true;
        }
    }

    private void Visualize(Path path)
    {
        Clear();  // マーカーも publisher.WayPointObjectList もクリア済み

        // ① ROS の最初の Pose の地上座標を取得（Unity座標系に合わせて符号を反転）
        var first = path.poses[0].pose.position;
        Vector3 rosStart = new Vector3(-(float)first.x, 0f, -(float)first.y);
        // ② startTransform.position を rosStart にマッチさせるオフセット
        Vector3 offset = startTransform.position - rosStart;


        for (int i = 0; i < path.poses.Length; i++)
    {
        var ps = path.poses[i];
            /*// ROS→Unity 座標変換
            Vector3 wp = new Vector3(
                -(float)ps.pose.position.x,
                originTransform.position.y,
                -(float)ps.pose.position.y
            );*/

            // ROS→Unity 座標変換（地面XZのみ）
            Vector3 rosPt = new Vector3(
            -(float)ps.pose.position.x,
            0f,
            -(float)ps.pose.position.y
                        );
            // ③ オフセットを適用
            Vector3 worldPt = new Vector3(
            rosPt.x + offset.x,
            startTransform.position.y,
            rosPt.z + offset.z
                        );

            var go = Instantiate(waypointPrefab, worldPt, Quaternion.identity, transform);


            /*
                        var go = Instantiate(waypointPrefab, startTransform);
                        go.transform.localPosition = rosPt;
                        go.transform.localRotation = Quaternion.identity;*/

            waypoints.Add(go);

        // ObjectManipulator 設定
        var manip = go.GetComponent<ObjectManipulator>() 
                 ?? go.AddComponent<ObjectManipulator>();
        int idx = i; // クロージャ対策
        manip.lastSelectExited.AddListener(_ => OnWaypointMoved(go, idx));

        // ライン頂点追加
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(idx, go.transform.position);

        // Publisher 登録
        publisher.WayPointObjectList.Add(go);
    }
}


    private void OnWaypointMoved(GameObject movedWaypoint, int index)
    {
        // ライン更新
        if (index < lineRenderer.positionCount)
            lineRenderer.SetPosition(index, movedWaypoint.transform.position);
    }

    private void Clear()
    {
        foreach (var wp in waypoints) Destroy(wp);
        waypoints.Clear();
        lineRenderer.positionCount = 0;
        publisher.WayPointObjectList.Clear();
    }

    private void ShowSuggestionMessage()
    {
        if (suggestionMessage == null || suggestionMessage.activeSelf) return;
        var cam = Camera.main;
        if (cam == null) return;

        suggestionMessage.transform.position = cam.transform.position + cam.transform.forward * messageDistance;
        suggestionMessage.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
        suggestionMessage.SetActive(true);
    }

    /// <summary>
    /// UIボタンなどから呼び出し、編集経路をROSへ送信
    /// </summary>
    public void PublishEditedRoute()
    {
        publisher.PublishStatus = true;
    }
}
