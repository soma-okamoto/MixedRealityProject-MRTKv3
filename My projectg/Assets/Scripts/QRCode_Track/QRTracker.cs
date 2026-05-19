using UnityEngine;
using TMPro;

public class QRTracker : MonoBehaviour
{
    public string QRid;
    public string RawQRid;

    [SerializeField] private TMP_Text label;

    private void Start()
    {
        ApplyLabel();
    }

    public void ApplyLabel()
    {
        if (label == null)
        {
            Debug.LogWarning($"QRTracker: label が設定されていません。QRid={QRid}");
            return;
        }

        if (QRid == "Main")
        {
            label.text = "Main QR Code";
            label.color = Color.red;
        }
        else if (!string.IsNullOrEmpty(QRid) && QRid.StartsWith("Sub"))
        {
            label.text = QRid;
            label.color = Color.blue;
        }
        else
        {
            label.text = $"Unknown QR Code: {QRid}";
            label.color = Color.gray;
        }
    }
}