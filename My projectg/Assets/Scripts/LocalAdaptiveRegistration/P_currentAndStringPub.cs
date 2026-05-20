using System.Collections.Generic;
using UnityEngine;

public class P_currentAndStringPub : MonoBehaviour
{
    [Header("References")]
    public BottleAreaChecker bottleAreaChecker;
    public P_cerrentPublisher p_currentPublisher;
    public PlaceCommandPublisher placeCommandPublisher;


    [Tooltip("座標変換の基準になるBase")]
    public GameObject origin_base;

    [Header("Debug")]
    public int currentId = -1;
    public GameObject currentBottle;
    public Vector3 currentWorldPosition;
    public float[] currentYoubotPosition;

        void Awake()
    {
        if (bottleAreaChecker == null)
            bottleAreaChecker = FindObjectOfType<BottleAreaChecker>();

        if (p_currentPublisher == null)
            p_currentPublisher = FindObjectOfType<P_cerrentPublisher>();

        if (placeCommandPublisher == null)
            placeCommandPublisher = FindObjectOfType<PlaceCommandPublisher>();

        if (origin_base == null)
        {
            origin_base = GameObject.Find("origin_base");
            if (origin_base == null)
            {
                Debug.LogError($" Hierarchy 上に名前 \"Origin\" の GameObject が見つかりません。");
            }
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

                Debug.Log(
                    $"[P_current] 自分を発見: ID={currentId}, " +
                    $"World={currentWorldPosition}, " +
                    $"YouBot=({currentYoubotPosition[0]}, {currentYoubotPosition[1]}, {currentYoubotPosition[2]})"
                );

                if (p_currentPublisher != null)
                {
                    p_currentPublisher.PublishCurrent(currentId, currentYoubotPosition);

                }
                else
                {
                    Debug.LogWarning("[P_current] P_currentPublisher が見つかりません");
                }

                if (placeCommandPublisher != null)
                {
                    placeCommandPublisher.PublishPlace();
                }
                else
                {
                    Debug.LogWarning("[P_current] PlaceCommandPublisher が見つかりません");
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