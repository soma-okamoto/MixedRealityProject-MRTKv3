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
    public GameObject rosConnector;


    [Tooltip("座標変換の基準になるBase")]
    public GameObject origin_base;

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

        if (origin_base == null)
        {
            origin_base = GameObject.Find("baseorigin_central");
            if (origin_base == null)
            {
                Debug.LogError($" Hierarchy 上に名前 \"baseorigin_central\" の GameObject が見つかりません。");
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

        // YouBot 向けに追加したいオフセット
        Vector3 axisOffset = new Vector3(0.0f, 0.0f, 0.0f);

        // Origin のワールド座標
        Vector3 originWorld = origin_base.transform.position;

        // ワールド差分で相対位置を計算
        Vector3 relative = bottleWorld - originWorld;

        // オフセットを加算
        Vector3 adjusted = relative + axisOffset;

        // // Unity → YouBot 座標変換
        // float youbot_x = -adjusted.x;
        // float youbot_y = -adjusted.z;
        // float youbot_z = adjusted.y;

        float amir_x = adjusted.z;
        float amir_y = -adjusted.x;      
        float amir_z = adjusted.y;

        // calculated.Add(youbot_x);
        // calculated.Add(youbot_y);
        // calculated.Add(youbot_z);

        calculated.Add(amir_x);
        calculated.Add(amir_y); 
        calculated.Add(amir_z);

        return calculated.ToArray();
    }
}