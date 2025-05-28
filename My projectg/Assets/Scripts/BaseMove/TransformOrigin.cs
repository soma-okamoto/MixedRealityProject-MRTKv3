using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformOrigin : MonoBehaviour
{
    [Header("トラッキング参照")]
    [SerializeField] private Transform head;        // ユーザーの頭基準（例：Camera.main.transform）

    [Header("キャリブレーション対象")]
    [SerializeField] private Transform origin;      // 位置・回転を動かす上位オブジェクト
    [SerializeField] private Transform boundingBox; // 中間ノード
    [SerializeField] private Transform child;       // 実際に下位で動かしているオブジェクト

    // Start 時に記録しておく「初期のローカルオフセット」
    private Vector3 defaultOriginLocal;
    private Vector3 defaultBoundingLocal;
    private Vector3 defaultChildLocal;
    private Vector3 defaultOffsetTotal;  // head を起点とした初期 child のワールドオフセット

    void Start()
    {
        // Hierarchy 上の各ローカル値をキャプチャ
        defaultOriginLocal = origin.localPosition;
        defaultBoundingLocal = boundingBox.localPosition;
        defaultChildLocal = child.localPosition;

        // head ローカル空間でのオフセット総和を計算
        defaultOffsetTotal = defaultOriginLocal
                           + defaultBoundingLocal
                           + defaultChildLocal;
    }

    /// <summary>
    /// 任意タイミングで呼ぶと、
    /// 「現在 child がどこにあっても」、
    /// child のローカル Transform を変えずに
    /// origin のみ動かして child を初期位置に合わせ込む
    /// </summary>
    public void CalibrateNow()
    {
        // １）boundingBox→child の現在ローカル合成 Transform を取得
        Vector3 combinedLocalPos = boundingBox.localPosition
                                + boundingBox.localRotation * child.localPosition;
        Quaternion combinedLocalRot = boundingBox.localRotation * child.localRotation;

        // ２）頭を基準とした「初期 child のワールド位置」
        Vector3 desiredChildWorldPos = head.TransformPoint(defaultOffsetTotal);

        // ３）頭の Y 軸回転（ヨー）のみを取り出す
        float headYaw = head.eulerAngles.y;
        Quaternion headYawOnly = Quaternion.Euler(0f, headYaw, 0f);

        // ４）desired の回転はピッチ／ロール０でヨーのみ反映
        Quaternion desiredChildWorldRot = headYawOnly;

        // ５）Origin の world 回転を逆算
        //    origin.worldRot * combinedLocalRot = desiredChildWorldRot
        Quaternion newOriginWorldRot = desiredChildWorldRot * Quaternion.Inverse(combinedLocalRot);

        // ６）Origin の world 位置を逆算
        //    origin.worldPos + newOriginWorldRot * combinedLocalPos = desiredChildWorldPos
        Vector3 newOriginWorldPos = desiredChildWorldPos
                                 - newOriginWorldRot * combinedLocalPos;

        // ７）Origin に一発セット
        origin.SetPositionAndRotation(newOriginWorldPos, newOriginWorldRot);
    }
}
