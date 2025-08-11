using UnityEngine;
using TMPro;
using RosSharp.RosBridgeClient; 

using RosSharp.Urdf;
using System.Collections.Generic;

using RosSharp.RosBridgeClient.MessageTypes.Std;   

public class ShowDetectScore : MonoBehaviour
{
    [Header("References")]
    public TMP_Text text;  
    private DetectBottle_score_subscriber scoreSubscriber ;

    [Header("Display")]
    public bool showUnits = false;
    public string units = ""; 
    public string numberFormat = "F3"; // 小数2桁

    [Header("Color (optional)")]
    public Color staticColor = Color.white;     // 単色
    public bool useGradient = false;            // グラデ使用ON/OFF
    public Gradient gradient;                   // スコア→色
    public float minScore = 0f, maxScore = 1f;  // グラデのレンジ

    void Awake()
    {
        if (scoreSubscriber == null)
            scoreSubscriber = FindObjectOfType<DetectBottle_score_subscriber>();

        if (text == null)
            text = GetComponent<TMP_Text>(); // 同じオブジェクトにTextがあれば自動取得
    }

    void Update()
    {
        if (text == null || scoreSubscriber == null) return;

        float score = scoreSubscriber.bottle_score; // 購読側で更新される前提
        if (float.IsNaN(score)) return;

        // 表示テキスト
        if (showUnits && !string.IsNullOrEmpty(units))
            text.text = $"{score.ToString(numberFormat)} {units}";
        else
            text.text = score.ToString(numberFormat);

        // 色
        if (useGradient && gradient != null)
        {
            float t = Mathf.InverseLerp(minScore, maxScore, score);
            text.color = gradient.Evaluate(t);
        }
        else
        {
            text.color = staticColor;
        }
    }
}
