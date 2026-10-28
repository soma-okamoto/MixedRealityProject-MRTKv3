using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit.Input;
using MixedReality.Toolkit.UX;
// using Microsoft.MixedReality.OpenXR;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine.XR.Interaction.Toolkit;


public class P_currentAndStringPub : MonoBehaviour
{
    [Header("References")]
    public BottleAreaChecker bottleAreaChecker;
    public P_cerrentPublisher p_currentPublisher;
    public PlaceCommandPublisher placeCommandPublisher;
    public Hold_commandPublisher holdCommandPublisher;
    public ColorImageTriggerPublisher colorImageTriggerPublisher;
    public ColorCameraInfoPublisher colorCameraInfoPublisher;
    public DepthImageTriggerPublisher depthImageTriggerPublisher;
    public DepthCameraInfoPublisher depthCameraInfoPublisher;
    public ColorDepthTransformPublisher colorDepthTransformPublisher;
    public CorrectedBottlePositionSubscriber correctedBottlePositionSubscriber;

    public GameObject rosConnector;


    [Tooltip("ボトルの相対座標変換の基準になるObjectList")]
    public GameObject objectList;

    [Header("Debug")]
    public int currentId = -1;
    public GameObject currentBottle;
    public Vector3 currentWorldPosition;
    public float[] currentYoubotPosition;

    private ObjectManipulator manipulator;

        void Awake()
    {
        if (bottleAreaChecker == null)
            bottleAreaChecker = FindObjectOfType<BottleAreaChecker>();

        if (p_currentPublisher == null)
            p_currentPublisher = FindObjectOfType<P_cerrentPublisher>();

        if (placeCommandPublisher == null)
            placeCommandPublisher = FindObjectOfType<PlaceCommandPublisher>();

        if (holdCommandPublisher == null)
            holdCommandPublisher = FindObjectOfType<Hold_commandPublisher>();

        if (colorImageTriggerPublisher == null)
            colorImageTriggerPublisher = FindObjectOfType<ColorImageTriggerPublisher>();

        if (colorCameraInfoPublisher == null)
            colorCameraInfoPublisher = FindObjectOfType<ColorCameraInfoPublisher>();

        if (depthImageTriggerPublisher == null)
            depthImageTriggerPublisher = FindObjectOfType<DepthImageTriggerPublisher>();
        if (depthCameraInfoPublisher == null)
            depthCameraInfoPublisher = FindObjectOfType<DepthCameraInfoPublisher>();
        if (colorDepthTransformPublisher == null)
            colorDepthTransformPublisher = FindObjectOfType<ColorDepthTransformPublisher>();

        if (correctedBottlePositionSubscriber == null)
            correctedBottlePositionSubscriber = FindObjectOfType<CorrectedBottlePositionSubscriber>();

        if (objectList == null)
        {
            objectList = GameObject.Find("ObjectList");
            if (objectList == null)
            {
                Debug.LogError("[P_current] Hierarchy上にObjectListが見つかりません。");
            }
        }
        if (rosConnector == null)
        {
            rosConnector = GameObject.Find("RosConnector");
            if (rosConnector == null)
            {
                Debug.LogError($" Hierarchy 上に名前 \"RosConnector\" の GameObject が見つかりません。");
            }
        }

        manipulator = GetComponent<ObjectManipulator>();

        if (manipulator != null)
        {
            manipulator.firstSelectEntered.AddListener(OnGrabStarted);
            manipulator.lastSelectExited.AddListener(OnGrabEnded);
        }
        else
        {
            Debug.LogWarning("[P_current] ObjectManipulator が見つかりません");
        }
        
    }

    private void OnGrabStarted(SelectEnterEventArgs args)
    {
        // Debug.Log("[P_current] Grab Started");

        if (holdCommandPublisher != null)
        {
            holdCommandPublisher.HoldStart();
        }
        else
        {
            Debug.LogWarning("[P_current] HoldCommandPublisher が見つかりません");
        }
    }

    private void OnGrabEnded(SelectExitEventArgs args)
    {
        // Debug.Log("[P_current] Grab Ended");

        if (holdCommandPublisher != null)
        {
            holdCommandPublisher.HoldStop();
        }
    }
        private void OnDestroy()
    {
        if (manipulator != null)
        {
            manipulator.firstSelectEntered.RemoveListener(OnGrabStarted);
            manipulator.lastSelectExited.RemoveListener(OnGrabEnded);
        }
    }



    public void P_currentSignals()
    {


        List<BottleAreaChecker.BottleAreaInfo> infos = bottleAreaChecker.bottleInfos;

        bool found = false;

        foreach (var info in infos)
        {
            // 自分自身の GameObject と一致する情報を探す
            if (info.bottle == this.gameObject)
            {
                currentId = info.id;
                currentBottle = info.bottle;
                currentWorldPosition = info.position;
                currentYoubotPosition = CalculatePosition(info.position);

                found = true;

                // Register this bottle before publishing P_current and PLACE.
                // The next ROS PoseStamped is then applied only to this bottle.
                if (correctedBottlePositionSubscriber != null &&
                    correctedBottlePositionSubscriber.isActiveAndEnabled)
                {
                    correctedBottlePositionSubscriber.ArmForBottle(
                        currentId,
                        currentBottle,
                        objectList != null ? objectList.transform : null);
                }
                else
                {
                    Debug.LogWarning(
                        "[P_current] CorrectedBottlePositionSubscriber が見つからないか無効です。");
                }

                //Debug.Log(
                //    $"[P_current] 自分を発見: ID={currentId}, " +
                //    $"World={currentWorldPosition}, " +
                //    $"YouBot=({currentYoubotPosition[0]}, {currentYoubotPosition[1]}, {currentYoubotPosition[2]})"
                //);

                if (p_currentPublisher != null && p_currentPublisher.isActiveAndEnabled)
                {
                    p_currentPublisher.PublishCurrent(currentId, currentYoubotPosition);

                }
                else
                {
                    Debug.LogWarning("[P_current] P_currentPublisher が見つかりません");
                }

                if (placeCommandPublisher != null && placeCommandPublisher.isActiveAndEnabled)
                {
                    placeCommandPublisher.PublishPlace();

                    // ROS側のYOLO処理開始トリガー(Place)を送った直後に、
                    // Color画像を数フレームだけ取得・配信する。
                    if (colorImageTriggerPublisher != null &&
                        colorImageTriggerPublisher.isActiveAndEnabled)
                    {
                        colorImageTriggerPublisher.CaptureAndPublish();
                    }
                    else
                    {
                        Debug.LogWarning("[P_current] ColorImageTriggerPublisher が見つかりません");
                    }

                    if (colorCameraInfoPublisher != null &&
                        colorCameraInfoPublisher.isActiveAndEnabled)
                    {
                        colorCameraInfoPublisher.PublishCameraInfo();
                    }
                    else
                    {
                        Debug.LogWarning("[P_current] ColorCameraInfoPublisher is missing or disabled.");
                    }

                    if (depthImageTriggerPublisher != null &&
                        depthImageTriggerPublisher.isActiveAndEnabled)
                    {
                        depthImageTriggerPublisher.CaptureAndPublish();
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[P_current] DepthImageTriggerPublisher が未設定または無効です。" +
                            $" referenceNull={depthImageTriggerPublisher == null}, " +
                            $" active={depthImageTriggerPublisher != null && depthImageTriggerPublisher.isActiveAndEnabled}");
                    }

                    if (depthCameraInfoPublisher != null &&
                        depthCameraInfoPublisher.isActiveAndEnabled)
                    {
                        depthCameraInfoPublisher.PublishCameraInfo();
                    }
                    else
                    {
                        Debug.LogWarning("[P_current] DepthCameraInfoPublisher is missing or disabled.");
                    }

                    if (colorDepthTransformPublisher != null &&
                        colorDepthTransformPublisher.isActiveAndEnabled)
                    {
                        colorDepthTransformPublisher.PublishTransform();
                    }
                    else
                    {
                        Debug.LogWarning("[P_current] ColorDepthTransformPublisher is missing or disabled.");
                    }

                }
                else
                {
                    Debug.LogWarning("[P_current] PlaceCommandPublisher が見つかりません");
                }

                // rosConnector.GetComponent<Hold_commandPublisher>().enabled = false;
                // PlaceしたらHold終了
                if (holdCommandPublisher != null)
                {
                    holdCommandPublisher.HoldStop();
                }

                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[P_current] {gameObject.name} は bottleInfos 内に見つかりませんでした");
        }
    }

    private float[] CalculatePosition(Vector3 bottleWorld)
    {
        var calculated = new List<float>();

        if (objectList == null)
        {
            Debug.LogError("[P_current] ObjectListが未設定です。");
            return calculated.ToArray();
        }

        // YouBot 向けに追加したいオフセット
        Vector3 axisOffset = new Vector3(0.0f, 0.0f, 0.0f);

        // ボトルの親であるObjectListのローカル座標で相対位置を計算
        Vector3 relative = objectList.transform.InverseTransformPoint(bottleWorld);

        // オフセットを加算
        Vector3 adjusted = relative + axisOffset;

        // Unity → YouBot 座標変換
        float youbot_x = -adjusted.x;
        float youbot_y = -adjusted.z;
        float youbot_z = adjusted.y;

        calculated.Add(youbot_x);
        calculated.Add(youbot_y);
        calculated.Add(youbot_z);

        return calculated.ToArray();
    }
}
