
/*
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
    public Transform waypointsContainer;  // ← 追加
    public GameObject Aligin;

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

 

        // LineRenderer を Waypoints コンテナの下に生成
        // (1) parent だけ指定してインスタンス化
        var lrObj = Instantiate(lineRendererPrefab, waypointsContainer);

        // (2) 親基準のローカル原点にリセット
        lrObj.transform.localPosition = Vector3.zero;
        lrObj.transform.localRotation = Quaternion.identity;

        // (3) LineRenderer コンポーネント取得＋初期化
        lineRenderer = lrObj.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        // Publisher 初期化
        publisher.WayPointObjectList = new List<GameObject>();
    }

    void Update()
    {
        if (subscriber.messagePath != null && subscriber.isDirty)
        {
            
            Visualize(subscriber.messagePath);
            ShowSuggestionMessage();
            
            hasVisualized = true;
            subscriber.isDirty = false;
        }
    }

    public void DisvisualWaypoint()
    {
        // 1) ウェイポイント群
        if (waypointPrefab)
            waypointPrefab.SetActive(false);   // 子もまとめて非アクティブ

        // 2) ライン描画を消す
        if (lineRendererPrefab)
        {
            lineRendererPrefab.SetActive(false);
        }
    }
    

    private void Visualize(Path path)
    {
        Clear();  // マーカーも publisher.WayPointObjectList もクリア済み
        

        // (A) 頂点数を start＋waypoints 数に合わせる
        int n = path.poses.Length;
        lineRenderer.positionCount = n + 1;

        // (B) １点目は startTransform
        lineRenderer.SetPosition(0, startTransform.position);


        for (int i = 0; i < path.poses.Length; i++)
    {
        var ps = path.poses[i];


            // // ROS→Unity 座標変換（地面XZのみ）
            // Vector3 rosPt = new Vector3(
            // -(float)ps.pose.position.x,
            // 0f,
            // -(float)ps.pose.position.y
            //             );

            //Amir座標変換
            Vector3 rosPt = new Vector3(-(float)ps.pose.position.y,0f,(float)ps.pose.position.x);


            // // (1) ROS→Unity のローカル座標に変換（地面XZのみ）
            // Vector3 rosLocal = new Vector3(
            //     -(float)ps.pose.position.x,  // ROS の x → Unity の -X
            //     0f,                          // 地面に固定
            //     -(float)ps.pose.position.y   // ROS の y → Unity の -Z
            // );

            //Amir用
            Vector3 rosLocal = new Vector3(-(float)ps.pose.position.y,0f,(float)ps.pose.position.x);



            // (2) 親だけ指定してインスタンス化
            var go = Instantiate(waypointPrefab, waypointsContainer);

            // (3) ローカル座標・回転を設定
            go.transform.localPosition = rosLocal;
            go.transform.localRotation = Quaternion.identity;

            waypoints.Add(go);

        // ObjectManipulator 設定
        var manip = go.GetComponent<ObjectManipulator>() 
                 ?? go.AddComponent<ObjectManipulator>();
        int idx = i; // クロージャ対策
        manip.lastSelectExited.AddListener(_ => OnWaypointMoved(go, idx));

            // (D) そのワールド位置を頂点 i+1 にセット
            Vector3 worldPos = go.transform.position;
            lineRenderer.SetPosition(i + 1, worldPos);


            // Publisher 登録
            publisher.WayPointObjectList.Add(go);
    }
    
        
}


    private void OnWaypointMoved(GameObject movedWaypoint, int index)
    {
        // index は Waypoint のループインデックス → Line 上では index+1
        int lineIdx = index + 1;
        if (lineIdx < lineRenderer.positionCount)
            lineRenderer.SetPosition(lineIdx, movedWaypoint.transform.position);
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
        
        Aligin.GetComponent<AlignToTarget>().enabled = false;
        publisher.PublishStatus = true;

    }
}
*/
using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit.SpatialManipulation;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;
using RosSharp.RosBridgeClient;

public class IRMRouteVisual : MonoBehaviour
{
[Header("References")]
public IRMRouteSubscriber subscriber;
public IRMRoutePathPublisher publisher;
public Transform waypointsContainer;
public GameObject Aligin;

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

private readonly List<GameObject> waypoints = new List<GameObject>();
private LineRenderer lineRenderer;
private bool hasVisualized = false;

private void Start()
{
    subscriber = subscriber ?? GetComponent<IRMRouteSubscriber>();
    publisher = publisher ?? GetComponent<IRMRoutePathPublisher>();

    if (subscriber == null)
    {
        Debug.LogError("[IRMRouteVisual] IRMRouteSubscriber が見つかりません。");
        enabled = false;
        return;
    }

    if (publisher == null)
    {
        Debug.LogError("[IRMRouteVisual] IRMRoutePathPublisher が見つかりません。");
        enabled = false;
        return;
    }

    if (waypointsContainer == null)
    {
        Debug.LogError("[IRMRouteVisual] waypointsContainer が未設定です。");
        enabled = false;
        return;
    }

    if (waypointPrefab == null || lineRendererPrefab == null)
    {
        Debug.LogError("[IRMRouteVisual] waypointPrefab または lineRendererPrefab が未設定です。");
        enabled = false;
        return;
    }

    GameObject lrObj = Instantiate(lineRendererPrefab, waypointsContainer);

    lrObj.transform.localPosition = Vector3.zero;
    lrObj.transform.localRotation = Quaternion.identity;

    lineRenderer = lrObj.GetComponent<LineRenderer>();

    if (lineRenderer == null)
    {
        Debug.LogError("[IRMRouteVisual] lineRendererPrefab に LineRenderer がありません。");
        enabled = false;
        return;
    }

    lineRenderer.positionCount = 0;

    publisher.WayPointObjectList = new List<GameObject>();
}

private void Update()
{
    if (subscriber.messagePath == null || !subscriber.isDirty)
        return;

    Visualize(subscriber.messagePath);
    ShowSuggestionMessage();

    hasVisualized = true;
    subscriber.isDirty = false;
}

public void DisvisualWaypoint()
{
    foreach (GameObject waypoint in waypoints)
    {
        if (waypoint != null)
        {
            waypoint.SetActive(false);
        }
    }

    if (lineRenderer != null)
    {
        lineRenderer.gameObject.SetActive(false);
    }
}

private void Visualize(PathMsg path)
{
    if (path == null || path.poses == null)
    {
        Debug.LogWarning("[IRMRouteVisual] 受信した PathMsg が空です。");
        return;
    }

    if (startTransform == null)
    {
        Debug.LogError("[IRMRouteVisual] startTransform が未設定です。");
        return;
    }

    Clear();

    int waypointCount = path.poses.Length;

    lineRenderer.gameObject.SetActive(true);
    lineRenderer.positionCount = waypointCount + 1;

    // 経路開始点
    lineRenderer.SetPosition(0, startTransform.position);

    for (int i = 0; i < waypointCount; i++)
    {
        var poseStamped = path.poses[i];

        if (poseStamped == null || poseStamped.pose == null)
        {
            Debug.LogWarning($"[IRMRouteVisual] path.poses[{i}] が不正です。");
            continue;
        }

        // ROS -> Unity 変換
        // IRMRoutePathPublisher 側:
        // ROS = (Unity.z, -Unity.x, Unity.y)
        //
        // 逆変換:
        // Unity = (-ROS.y, ROS.z, ROS.x)
        //
        // 元コードと同様に、表示は地面上へ固定するため Unity.y は 0 とする。
        Vector3 rosLocal = new Vector3(
            -(float)poseStamped.pose.position.y,
            0f,
            (float)poseStamped.pose.position.x
        );

        GameObject waypoint = Instantiate(waypointPrefab, waypointsContainer);

        waypoint.transform.localPosition = rosLocal;
        waypoint.transform.localRotation = Quaternion.identity;

        waypoints.Add(waypoint);

        ObjectManipulator manipulator =
            waypoint.GetComponent<ObjectManipulator>();

        if (manipulator == null)
        {
            manipulator = waypoint.AddComponent<ObjectManipulator>();
        }

        int waypointIndex = i;

        manipulator.lastSelectExited.AddListener(
            _ => OnWaypointMoved(waypoint, waypointIndex)
        );

        lineRenderer.SetPosition(i + 1, waypoint.transform.position);

        publisher.WayPointObjectList.Add(waypoint);
    }
}

private void OnWaypointMoved(GameObject movedWaypoint, int index)
{
    if (lineRenderer == null || movedWaypoint == null)
        return;

    int lineIndex = index + 1;

    if (lineIndex < lineRenderer.positionCount)
    {
        lineRenderer.SetPosition(
            lineIndex,
            movedWaypoint.transform.position
        );
    }
}

private void Clear()
{
    foreach (GameObject waypoint in waypoints)
    {
        if (waypoint != null)
        {
            Destroy(waypoint);
        }
    }

    waypoints.Clear();

    if (lineRenderer != null)
    {
        lineRenderer.positionCount = 0;
    }

    if (publisher != null && publisher.WayPointObjectList != null)
    {
        publisher.WayPointObjectList.Clear();
    }
}

private void ShowSuggestionMessage()
{
    if (suggestionMessage == null || suggestionMessage.activeSelf)
        return;

    Camera mainCamera = Camera.main;

    if (mainCamera == null)
        return;

    suggestionMessage.transform.position =
        mainCamera.transform.position +
        mainCamera.transform.forward * messageDistance;

    suggestionMessage.transform.rotation =
        Quaternion.LookRotation(mainCamera.transform.forward);

    suggestionMessage.SetActive(true);
}

public void PublishEditedRoute()
{
    if (Aligin != null)
    {
        AlignToTarget alignToTarget =
            Aligin.GetComponent<AlignToTarget>();

        if (alignToTarget != null)
        {
            alignToTarget.enabled = false;
        }
    }

    if (publisher != null)
    {
        publisher.PublishStatus = true;
    }
}


}
