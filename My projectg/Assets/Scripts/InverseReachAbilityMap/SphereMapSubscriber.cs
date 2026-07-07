/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
    public class SphereMapSubscriber : UnitySubscriber<Float32MultiArray>
    {
        [Header("Sphere Settings")]
        public GameObject pointPrefab;    // 球体プレハブ
        public Gradient colorByScore;     // スコア→色変換
        public float sphereScale = 0.02f; // 球体サイズ
        public int batchSize = 1000;      // フレーム分割数

      

        const float baseOffsetX = 0.0f;
        const float baseOffsetY = 0.0f;
        const float baseOffsetZ = 0.0f;



        private float[] latestData;
        private bool dataReceived = false;
        private object dataLock = new object();

        private GameObject parent;
        private List<GameObject> pool = new List<GameObject>();

        protected override void Start()
        {
            base.Start();
            parent = new GameObject("InverseReachMapSpheres");
            parent.transform.SetParent(transform, false);
            if (pointPrefab == null) Debug.LogError("pointPrefab が設定されていません。");
        }

        protected override void ReceiveMessage(Float32MultiArray message)
        {
            lock (dataLock)
            {
                latestData = message.data;
                dataReceived = true;
            }
        }

        void Update()
        {
            if (!dataReceived) return;

            float[] dataCopy;
            lock (dataLock)
            {
                dataCopy = latestData;
                dataReceived = false;
            }

            int count = dataCopy.Length / 4;

            // プールに必要分を確保
            if (pool.Count < count)
            {
                for (int i = pool.Count; i < count; i++)
                {
                    var go = Instantiate(pointPrefab, parent.transform);
                    go.transform.localScale = Vector3.one * sphereScale;
                    pool.Add(go);
                }
            }
            // 余剰オブジェクトは非アクティブ化
            for (int i = count; i < pool.Count; i++)
                pool[i].SetActive(false);

            StartCoroutine(UpdateSpheres(dataCopy, count));
        }

        private IEnumerator UpdateSpheres(float[] data, int count)
        {
            // スコアの min/max を算出
            float minScore = float.MaxValue, maxScore = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float s = data[i * 4 + 3];
                if (s < minScore) minScore = s;
                if (s > maxScore) maxScore = s;
            }

            for (int i = 0; i < count; i++)
            {
                var go = pool[i];
                go.SetActive(true);

                float rx = data[i * 4 + 0] + baseOffsetX;
                float ry = data[i * 4 + 1] + baseOffsetY;
                float rz = data[i * 4 + 2] + baseOffsetZ;
                float score = data[i * 4 + 3];

                Vector3 pos = new Vector3(-rx, rz, -ry);


                go.transform.localPosition = pos;

                float t = Mathf.InverseLerp(minScore, maxScore, score);
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                    rend.material.color = colorByScore.Evaluate(t);

                if (i % batchSize == 0)
                    yield return null;
            }
        }
    }
}
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace RosSharp.RosBridgeClient
{
public class SphereMapSubscriber : MonoBehaviour
{
[Header("ROS 2 Topic")]
[FormerlySerializedAs("Topic")]
[SerializeField] private string topicName = "/IRM_Map";

    [Header("Sphere Settings")]
    public GameObject pointPrefab;
    public Gradient colorByScore;
    public float sphereScale = 0.02f;
    public int batchSize = 1000;

    const float baseOffsetX = 0.0f;
    const float baseOffsetY = 0.0f;
    const float baseOffsetZ = 0.0f;

    private float[] latestData;
    private bool dataReceived;
    private readonly object dataLock = new object();

    private GameObject parent;
    private readonly List<GameObject> pool = new List<GameObject>();

    private ROSConnection ros;
    private Coroutine updateCoroutine;

    private void Start()
    {
        if (pointPrefab == null)
        {
            Debug.LogError("[SphereMapSubscriber] pointPrefab が設定されていません。");
            enabled = false;
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();

        // ROS 2: std_msgs/msg/Float32MultiArray
        ros.Subscribe<Float32MultiArrayMsg>(topicName, ReceiveMessage);

        parent = new GameObject("InverseReachMapSpheres");
        parent.transform.SetParent(transform, false);

        Debug.Log(
            $"[SphereMapSubscriber] ROS-TCP subscriber registered: " +
            $"topic={topicName}, type=std_msgs/Float32MultiArray");
    }

    private void ReceiveMessage(Float32MultiArrayMsg message)
    {
        if (message == null || message.data == null)
        {
            Debug.LogWarning(
                "[SphereMapSubscriber] null の Float32MultiArray を受信しました。");
            return;
        }

        lock (dataLock)
        {
            // 次の受信で元の配列が再利用・変更されても影響しないよう複製
            latestData = (float[])message.data.Clone();
            dataReceived = true;
        }
    }

    private void Update()
    {
        if (!dataReceived)
            return;

        float[] dataCopy;

        lock (dataLock)
        {
            dataCopy = latestData;
            dataReceived = false;
        }

        if (dataCopy == null || dataCopy.Length == 0)
        {
            ClearActiveSpheres();
            return;
        }

        if (dataCopy.Length % 4 != 0)
        {
            Debug.LogWarning(
                $"[SphereMapSubscriber] 受信データ数={dataCopy.Length} は4の倍数ではありません。 " +
                "末尾の不足要素は無視します。");
        }

        int count = dataCopy.Length / 4;

        EnsurePoolSize(count);
        DisableExtraSpheres(count);

        // 前回の描画Coroutineが残っていると、古いマップが後から上書きする。
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }

        updateCoroutine = StartCoroutine(UpdateSpheres(dataCopy, count));
    }

    private void EnsurePoolSize(int count)
    {
        if (pool.Count >= count)
            return;

        for (int i = pool.Count; i < count; i++)
        {
            GameObject go = Instantiate(pointPrefab, parent.transform);
            go.transform.localScale = Vector3.one * sphereScale;
            pool.Add(go);
        }
    }

    private void DisableExtraSpheres(int activeCount)
    {
        for (int i = activeCount; i < pool.Count; i++)
        {
            if (pool[i] != null)
            {
                pool[i].SetActive(false);
            }
        }
    }

    private void ClearActiveSpheres()
    {
        foreach (GameObject sphere in pool)
        {
            if (sphere != null)
            {
                sphere.SetActive(false);
            }
        }
    }

    private IEnumerator UpdateSpheres(float[] data, int count)
    {
        if (count <= 0)
        {
            yield break;
        }

        float minScore = float.MaxValue;
        float maxScore = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            float score = data[i * 4 + 3];

            if (score < minScore)
                minScore = score;

            if (score > maxScore)
                maxScore = score;
        }

        int resolvedBatchSize = Mathf.Max(1, batchSize);

        for (int i = 0; i < count; i++)
        {
            GameObject go = pool[i];

            if (go == null)
                continue;

            go.SetActive(true);

            float rx = data[i * 4 + 0] + baseOffsetX;
            float ry = data[i * 4 + 1] + baseOffsetY;
            float rz = data[i * 4 + 2] + baseOffsetZ;
            float score = data[i * 4 + 3];

            // 元コードのROS -> Unity変換を維持
            // ROS:   (rx, ry, rz)
            // Unity: (-rx, rz, -ry)
            Vector3 unityPosition = new Vector3(
                -rx,
                rz,
                -ry
            );

            go.transform.localPosition = unityPosition;

            float normalizedScore = Mathf.Approximately(minScore, maxScore)
                ? 0.5f
                : Mathf.InverseLerp(minScore, maxScore, score);

            Renderer renderer = go.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = colorByScore != null
                    ? colorByScore.Evaluate(normalizedScore)
                    : Color.white;
            }

            if ((i + 1) % resolvedBatchSize == 0)
            {
                yield return null;
            }
        }

        updateCoroutine = null;
    }
}
}