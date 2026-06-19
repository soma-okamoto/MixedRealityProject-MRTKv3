using MixedReality.Toolkit.SpatialManipulation;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class ObjectDuplicator : MonoBehaviour
{
    [Tooltip("複製したい元のオブジェクト")]
    public GameObject objectToDuplicate;

    [Tooltip("複製時に位置をどれだけずらすか")]
    public Vector3 offset = new Vector3(1, 0, 0);

    [Tooltip("複製時にどれだけ回転させるか (Euler 角度)")]
    public Vector3 rotationEuler = Vector3.zero;

    [Tooltip("複製時のスケール")]
    public Vector3 scale = Vector3.one;
    // youbotを一時的に非アクティブにしたリスト
    private List<GameObject> duplicatedYoubots = new List<GameObject>();
    [Tooltip("複製先の親オブジェクト（空のTransformなど）")]
    public Transform targetParent;

    [Header("複製後に残す対象")]
    [Tooltip("このタグを持つオブジェクトは複製後も残す")]
    public List<string> keepTags = new List<string>() { "bottle", "Origin" };

    [Tooltip("名前で残したい任意オブジェクト")]
    public List<string> keepObjectNames = new List<string>();



    public void DuplicateObject()
    {
        GameObject duplicatedObject = null; // ← ここで宣言しておく

        if (objectToDuplicate != null)
        {
            // Rotationは元の回転 + 指定回転
            Quaternion newRotation = objectToDuplicate.transform.rotation * Quaternion.Euler(rotationEuler);

            // 複製して新しい位置・回転で生成
            duplicatedObject = Instantiate(
                objectToDuplicate,
                objectToDuplicate.transform.position + offset,
                newRotation,
                targetParent // ← Inspectorで指定されたオブジェクト配下に生成される
            );

            duplicatedObject.name = objectToDuplicate.name + "_Copy";
            // 複製された BoundingBox 本体のコンポーネントを有効化
            var bc = duplicatedObject.GetComponent<BoundsControl>();
            if (bc != null) bc.enabled = true;

            var om = duplicatedObject.GetComponent<ObjectManipulator>();
            if (om != null) om.enabled = true;

            var col = duplicatedObject.GetComponent<BoxCollider>();
            if (col != null) col.enabled = true;

            // RotationAxisConstraint を無効化
            var rotationConstraint = duplicatedObject.GetComponent<RotationAxisConstraint>();
            if (rotationConstraint != null)
            {
                rotationConstraint.enabled = false;
            }
            // Scaleは指定したものを適用（元のスケールを使いたいなら objectToDuplicate.transform.localScale に変更可）
            duplicatedObject.transform.localScale = scale;

            //// PointCloudRenderer の subscriber の設定をコピー
            //var originalRenderer = objectToDuplicate.GetComponentInChildren<PointCloudRenderer>();
            //var originalSubscriber = originalRenderer?.subscriber;

            //var duplicatedRenderer = duplicatedObject.GetComponentInChildren<PointCloudRenderer>();
            //if (duplicatedRenderer != null && originalSubscriber != null)
            //{
            //    duplicatedRenderer.subscriber = originalSubscriber;
            //}
            // 2. 必要な子だけ残す
            RemoveUnwantedChildren(duplicatedObject);

            //DeactivateWaypointsInHierarchy(duplicatedObject);

            
            //HandleYoubotActivation(objectToDuplicate, duplicatedObject);
        }
        else
        {
            UnityEngine.Debug.LogWarning("objectToDuplicateが設定されていません！");
        }
        // ここで使えるようになる
        if (duplicatedObject != null)
        {
            //DeactivateWaypointsInHierarchy(duplicatedObject);
            // Instantiate直後に複製したBoundingBoxの中のボトルのタグを変更
            Transform[] allChildren = duplicatedObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.CompareTag("bottle"))
                {
                    child.tag = "SubBottle";

                    //KeepOnlyVisualComponentsOnSubBottle(child.gameObject);
                    BottleAreaState state = child.GetComponent<BottleAreaState>();
                    BottleIdentity id = child.GetComponent<BottleIdentity>();
                    P_currentAndStringPub p = child.GetComponent<P_currentAndStringPub>();

                    if (state != null)
                    {
                        Destroy(state);
                    }
                    if (id != null)
                    {
                        Destroy(id);
                    }
                    if (p != null)
                    {
                        Destroy(p);
                    }

                }
            }

            foreach (Transform child in duplicatedObject.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("Origin"))
                {
                    child.tag = "SubOrigin";
                    //Debug.Log($"子オブジェクトタグ変更: {child.name} → SubOrigin");
                }
            }


            // RadialView制御スクリプトを追加
            var toggleScript = duplicatedObject.AddComponent<RadialViewToggleOnManipulation>();
            var manipulator = duplicatedObject.GetComponent<ObjectManipulator>();
            var radialView = duplicatedObject.transform.parent?.GetComponent<RadialView>();

            toggleScript.interactable = manipulator;
            toggleScript.radialView = radialView;

        }
        SetTopMostFirstActive_OthersInactive();


        if (duplicatedObject != null)
        {
            // BottleSync に parentB を渡す
            var bottleSync = FindObjectOfType<BottleSync>();
            if (bottleSync != null)
            {
                bottleSync.SetParentB(duplicatedObject.transform);
            }

            var originSync = FindObjectOfType<OriginSync>();
            if (originSync != null)
            {
                originSync.SetParentB(duplicatedObject.transform);
            }
        }


    }
    

    void SetTopMostFirstActive_OthersInactive()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        List<GameObject> matches = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "BoundingBoxWithTraditionalHandles(Clone)" && obj.hideFlags == HideFlags.None)
            {
                matches.Add(obj);
            }
        }

        if (matches.Count == 0)
        {
            UnityEngine.Debug.LogWarning("BoundingBoxWithTraditionalHandles(Clone) が見つかりませんでした！");
            return;
        }

        // 階層が浅い順にソートし、同じ深さなら見つかった順（=そのままの順）
        matches.Sort((a, b) =>
        {
            int depthA = GetHierarchyDepth(a.transform);
            int depthB = GetHierarchyDepth(b.transform);
            return depthA.CompareTo(depthB); // depth が浅いほど先に
        });

        // 最初の1つをアクティブ、それ以外を非アクティブ
        bool found = false;
        foreach (var obj in matches)
        {
            if (!found)
            {
                obj.SetActive(true);
                found = true;
                UnityEngine.Debug.Log($"アクティブ化: {obj.name} ({GetHierarchyPath(obj.transform)})");
            }
            else
            {
                obj.SetActive(false);
            }
        }
    }

    int GetHierarchyDepth(Transform t)
    {
        int depth = 0;
        while (t.parent != null)
        {
            depth++;
            t = t.parent;
        }
        return depth;
    }

    string GetHierarchyPath(Transform t)
    {
        List<string> path = new List<string>();
        while (t != null)
        {
            path.Insert(0, t.name);
            t = t.parent;
        }
        return string.Join("/", path);
    }
    //void DeactivateWaypointsInHierarchy(GameObject root)
    //{
    //    Transform[] children = root.GetComponentsInChildren<Transform>(true); // 非アクティブな子も含めて取得
    //    foreach (Transform child in children)
    //    {
    //        if (child.name == "WayPoints")
    //        {
    //            child.gameObject.SetActive(false);
    //            UnityEngine.Debug.Log($"Waypointsを非アクティブ化: {GetHierarchyPath(child)}");
    //        }
    //    }
    //}


    void RemoveUnwantedChildren(GameObject duplicatedRoot)
    {
        if (duplicatedRoot == null) return;

        Transform[] allChildren = duplicatedRoot.GetComponentsInChildren<Transform>(true);
        List<GameObject> destroyTargets = new List<GameObject>();

        foreach (Transform child in allChildren)
        {
            if (child == duplicatedRoot.transform)
                continue;

            // 残す対象そのもの
            // 残す対象の親
            // 残す対象の子
            // このどれかなら残す
            if (ShouldKeepTransform(child, duplicatedRoot.transform))
                continue;

            destroyTargets.Add(child.gameObject);
        }

        // 深い階層から削除
        destroyTargets.Sort((a, b) =>
        {
            int depthA = GetHierarchyDepth(a.transform);
            int depthB = GetHierarchyDepth(b.transform);
            return depthB.CompareTo(depthA);
        });

        foreach (GameObject obj in destroyTargets)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }


    bool ShouldKeepTransform(Transform target, Transform root)
    {
        // 自分自身が残す対象
        if (IsKeepTarget(target))
            return true;

        // 親のどこかが残す対象なら、その子も残す
        Transform p = target.parent;
        while (p != null && p != root.parent)
        {
            if (IsKeepTarget(p))
                return true;

            if (p == root)
                break;

            p = p.parent;
        }

        // 子孫に残す対象があるなら、その親も残す
        Transform[] children = target.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == target) continue;

            if (IsKeepTarget(child))
                return true;
        }

        return false;
    }

    bool IsKeepTarget(Transform target)
    {
        if (target == null) return false;

        // タグで残す
        foreach (string tagName in keepTags)
        {
            if (string.IsNullOrEmpty(tagName)) continue;

            if (target.CompareTag(tagName))
                return true;
        }

        // 名前で残す
        foreach (string objectName in keepObjectNames)
        {
            if (string.IsNullOrEmpty(objectName)) continue;

            if (target.name == objectName)
                return true;
        }

        return false;
    }

    void KeepOnlyVisualComponentsOnSubBottle(GameObject bottle)
    {
        if (bottle == null) return;

        Component[] components = bottle.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null) continue;

            // Transform は消せないので残す
            if (component is Transform) continue;

            // 見た目に必要なものは残す
            if (component is MeshRenderer) continue;
            if (component is MeshFilter) continue;

            // SkinnedMeshRenderer を使っている場合に備えて残す
            if (component is SkinnedMeshRenderer) continue;

            // それ以外は削除
            Destroy(component);
        }
    }


}
