
using UnityEngine;
using System.Collections.Generic;

public class BottleSync : MonoBehaviour
{
    public Transform parentA; // マスターbottleの親オブジェクト (ParentA)
    public Transform parentB; // サブbottleの親オブジェクト (ParentB)

    private Dictionary<GameObject, GameObject> masterToSubMapping = new Dictionary<GameObject, GameObject>();


    private Dictionary<GameObject, Color> masterColors = new Dictionary<GameObject, Color>();
    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();
    private Dictionary<PointCloudRenderer, PointCloudRenderer> masterPCLToSubPCL = new();


    void Start()
    {
        if (parentA == null || parentB == null)
        {
            UnityEngine.Debug.Log("ParentAまたはParentBが設定されていません！");
            return;
        }

        List<GameObject> masterList = GetOrderedChildrenWithTag(parentA, "bottle");
        List<GameObject> subList = GetOrderedChildrenWithTag(parentB, "SubBottle");


        // PointCloudRenderer 同士も順序対応でペアリング
        PointCloudRenderer[] masterPCLs = parentA.GetComponentsInChildren<PointCloudRenderer>();
        PointCloudRenderer[] subPCLs = parentB.GetComponentsInChildren<PointCloudRenderer>();

        int pclPairCount = Mathf.Min(masterPCLs.Length, subPCLs.Length);
        for (int i = 0; i < pclPairCount; i++)
        {
            masterPCLToSubPCL[masterPCLs[i]] = subPCLs[i];
        }



    }

    public void SetParentB(Transform newParentB)
    {
        parentB = newParentB;

        // 並び順に従って取得
        List<GameObject> masterList = GetOrderedChildrenWithTag(parentA, "bottle");
        List<GameObject> subList = GetOrderedChildrenWithTag(parentB, "SubBottle");


        masterToSubMapping.Clear();

        originalColors.Clear();

        int pairCount = Mathf.Min(masterList.Count, subList.Count);
        for (int i = 0; i < pairCount; i++)
        {
            GameObject master = masterList[i];
            GameObject sub = subList[i];

            masterToSubMapping[master] = sub;

            //BottleAreaState masterState = master.GetComponent<BottleAreaState>();
            //BottleAreaState subState = sub.GetComponent<BottleAreaState>();

            //if (masterState != null)
            //{
            //    masterState.SetVisualOnly(false);
            //}

            //if (subState != null)
            //{
            //    subState.SetVisualOnly(true);
            //}

            Renderer subRenderer = sub.GetComponent<Renderer>();
            if (subRenderer != null)
            {
                originalColors[sub] = subRenderer.material.color;
            }
        }


        // SetParentB の末尾にこれがあるか
        PointCloudRenderer[] masterPCLs = parentA.GetComponentsInChildren<PointCloudRenderer>();
        PointCloudRenderer[] subPCLs = parentB.GetComponentsInChildren<PointCloudRenderer>();
        masterPCLToSubPCL.Clear();
        for (int i = 0; i < Mathf.Min(masterPCLs.Length, subPCLs.Length); i++)
        {
            masterPCLToSubPCL[masterPCLs[i]] = subPCLs[i];
        }


    }


    public Dictionary<GameObject, GameObject> GetMasterToSubMapping()
    {
        return masterToSubMapping;
    }


    void Update()
    {
        foreach (var entry in masterToSubMapping)
        {
            GameObject masterBottle = entry.Key;
            GameObject subBottle = entry.Value;

            if (masterBottle == null || subBottle == null)
                continue;

            // =========================
            // 位置・回転同期
            // =========================
            Transform masterParent = masterBottle.transform.parent;

            Vector3 masterLocalPosition =
                masterParent.InverseTransformPoint(masterBottle.transform.position);

            Quaternion masterLocalRotation =
                Quaternion.Inverse(masterParent.rotation) * masterBottle.transform.rotation;

            subBottle.transform.localPosition = masterLocalPosition;
            subBottle.transform.localRotation = masterLocalRotation;

            // =========================
            // 色・Material同期
            // =========================
            BottleAreaState masterState = masterBottle.GetComponent<BottleAreaState>();
            Renderer subRenderer = subBottle.GetComponentInChildren<Renderer>();

            if (masterState != null && subRenderer != null)
            {
                Material subMat = subRenderer.material;

                string subColorProp = subMat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

                subMat.SetColor(subColorProp, masterState.CurrentColor);
            }
        }
    }

    public GameObject GetMasterFromSub(GameObject sub)
    {
        foreach (var kvp in masterToSubMapping)
        {
            if (kvp.Value == sub)
                return kvp.Key;
        }
        return null;
    }


    // 指定された親オブジェクトの子オブジェクトの中から特定のタグを持つものを取得
    private List<GameObject> GetOrderedChildrenWithTag(Transform parent, string tag)
    {
        List<GameObject> result = new List<GameObject>();

        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child != parent && child.CompareTag(tag))  // 自分自身は除く
            {
                result.Add(child.gameObject);
            }
        }

        return result;
    }

    public bool TryGetSubFromMaster(GameObject master, out GameObject sub)
    {
        return masterToSubMapping.TryGetValue(master, out sub);
    }

    public PointCloudRenderer GetSubPointCloud(PointCloudRenderer masterPCL)
    {
        masterPCLToSubPCL.TryGetValue(masterPCL, out var sub);
        return sub;
    }

    public PointCloudRenderer GetMasterPointCloud(PointCloudRenderer subPCL)
    {
        foreach (var kvp in masterPCLToSubPCL)
        {
            if (kvp.Value == subPCL)
                return kvp.Key;
        }
        return null;
    }
    // SubBottle（＝このスクリプトがついてるGameObject） → 対応する Sub の PointCloudRenderer を取得
    public PointCloudRenderer GetSubPointCloudFromSubBottle(GameObject subBottle)
    {
        return subBottle.GetComponentInChildren<PointCloudRenderer>();
    }

    private GameObject currentHitObject;

    public void SetCurrentHitObject(GameObject obj)
    {
        currentHitObject = obj;
    }

    public GameObject GetCurrentHitObject()
    {
        return currentHitObject;
    }


    void CopyMaterialRenderingSettings(Material source, Material target)
    {
        if (source == null || target == null) return;

        string[] floatProps =
        {
        "_Surface",
        "_SrcBlend",
        "_DstBlend",
        "_ZWrite"
    };

        foreach (string prop in floatProps)
        {
            if (source.HasProperty(prop) && target.HasProperty(prop))
            {
                target.SetFloat(prop, source.GetFloat(prop));
            }
        }

        target.renderQueue = source.renderQueue;

        if (source.IsKeywordEnabled("_ALPHABLEND_ON"))
            target.EnableKeyword("_ALPHABLEND_ON");
        else
            target.DisableKeyword("_ALPHABLEND_ON");

        if (source.IsKeywordEnabled("_ALPHATEST_ON"))
            target.EnableKeyword("_ALPHATEST_ON");
        else
            target.DisableKeyword("_ALPHATEST_ON");

        if (source.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"))
            target.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        else
            target.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

}

