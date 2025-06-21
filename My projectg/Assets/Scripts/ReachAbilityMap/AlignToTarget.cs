using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignToTarget : MonoBehaviour
{
    [Tooltip("このオブジェクト（A）の位置を合わせたい対象 (B)")]
    public Transform targetTransform;

    void Update()
    {
        if (targetTransform != null)
        {
            // ワールド座標を丸ごとコピー
            transform.position = targetTransform.position;

            // もし向きも揃えたいならこちらも
            // transform.rotation = targetTransform.rotation;
        }
    }
}
