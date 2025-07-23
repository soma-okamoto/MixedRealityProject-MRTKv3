using UnityEngine;

/// <summary>
/// AreaChecker から inside/outside の情報だけを受け取って保持するコンポーネント。
/// 色変更などのビジュアル処理は行わない。
/// </summary>
//[RequireComponent(typeof(Renderer))]
public class BottleAreaState : MonoBehaviour
{
    // 現在の inside / outside の状態
    [field: SerializeField, Tooltip("エリア内かどうか（読み取り専用）")]
    public bool IsInside { get; private set; }
    public bool IsHit { get; private set; }
    public ObjectHit ObjectHit;

    [ColorUsage(false, true)] public Color PickColor;      // Inside ＆ Hit
    [ColorUsage(false, true)] public Color OtherColor;     // Outside
    private Color originalColor;

    public float fadeSpeed = 5f;
    // 内部キャッシュ
    private Material mat;
    private string colorProp;
    private Coroutine fadeRoutine;
    public GameObject targetUIPrefab;
    private GameObject currentTargetUI = null;
    public bool isHitted = false;
    private bool isTransparent = false;

    [Range(0f, 1f)] public float otherAlpha = 0.2f;


    void Awake()
    {
        // 1) Renderer の sharedMaterial をコピーして独立
        var rend = GetComponent<Renderer>();
        mat = new Material(rend.sharedMaterial);
        rend.material = mat;

        // 2) カラープロパティ名を自動判別
        colorProp = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

        // 3) 本来の色をマテリアルから取得
        originalColor = mat.GetColor(colorProp);
        // シーン中から ObjectHit を自動取得
        if (ObjectHit == null)
            ObjectHit = FindObjectOfType<ObjectHit>();
        IsInside = true;
    }


    public void SetInside(bool isInside)
    {
        IsInside = isInside;
    }
    public void SetHit(bool hit)
    {
        IsHit = hit;
    }


    public void Update()
    {
        GameObject globalHit = ObjectHit != null ? ObjectHit.hitObject : null;
        bool anyOtherHit = (globalHit != null) && !IsHit;


        // ── 透明モード判定 ──
        bool shouldBeTransparent = !IsInside || anyOtherHit;
        if (shouldBeTransparent != isTransparent)
        {
            if (shouldBeTransparent) SetMaterialTransparent(mat);
            else SetMaterialOpaque(mat);
            isTransparent = shouldBeTransparent;
        }



        Color targetColor;


        if (!IsInside || anyOtherHit)
        {
            //UnityEngine.Debug.Log("Outside:position"+position);
              targetColor = new Color(
            OtherColor.r,
            OtherColor.g,
            OtherColor.b,
            otherAlpha
        );
            DestroyUI();

        }
        else if (IsHit)
        {
            //UnityEngine.Debug.Log("Inside:position"+position);
            targetColor = PickColor;
            if (!isHitted)
            {
                UIset();
                isHitted = true;
            }

        }
        else
        {
            // �G���A���������̃{�g�����I��
            targetColor = originalColor;
            DestroyUI();

        }
        Color current = mat.GetColor(colorProp);
        Color next = Color.Lerp(current, targetColor, Time.deltaTime * fadeSpeed);
        mat.SetColor(colorProp, next);

    }

    public void UIset()
    {
        if (currentTargetUI != null)
        {
            Destroy(currentTargetUI);
            currentTargetUI = null;
        }

        Vector3 bottlePos = transform.position;
        Transform camT = Camera.main.transform;
        Vector3 dirToCam = (camT.position - bottlePos).normalized;

        float forwardOffset = 0.15f;  // 10cm ��O��                                      
        float heightOffset = 0.1f;   // 10cm ���

        Vector3 uiPos = bottlePos
                        + dirToCam * forwardOffset
                        + Vector3.up * heightOffset;


        Vector3 toCamFlat = camT.position - uiPos;
        toCamFlat.y = 0;
        Quaternion uiRot = Quaternion.LookRotation(toCamFlat.normalized, Vector3.up);
        currentTargetUI = Instantiate(
                                        targetUIPrefab,
                                        uiPos,
                                        uiRot,
                                        this.transform
                                    );
        currentTargetUI.transform.LookAt(camT.position, Vector3.up);

    }
    public void DestroyUI()
    {
        if (currentTargetUI != null)
        {
            Destroy(currentTargetUI);
            currentTargetUI = null;
        }
        isHitted = false;
    }
    
      // ─────────────────────────────────────────────────
    void SetMaterialTransparent(Material m)
    {
        m.SetFloat("_Surface", 1);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    void SetMaterialOpaque(Material m)
    {
        m.SetFloat("_Surface", 0);
        m.SetOverrideTag("RenderType", "Opaque");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
    }
    
}


