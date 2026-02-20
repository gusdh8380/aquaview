using UnityEngine;
using TMPro;

/// <summary>
/// 탱크(StageView) 위에 공정명 텍스트를 월드 공간에 표시합니다.
///
/// [사용 방법]
///   StageView GameObject에 이 컴포넌트를 추가하면 자동으로
///   World Space Canvas + TextMeshPro 텍스트를 생성합니다.
///   별도 Prefab/Inspector 연결 불필요.
///
/// [Inspector 설정]
///   stageIndex  : 공정 인덱스 (0~4), StageView와 동일하게 설정
///   labelOffset : 탱크 중심으로부터의 텍스트 위치 오프셋 (기본 0, 1, 0)
///   labelScale  : 텍스트 크기 배율 (기본 0.012)
///   faceCamera  : true면 항상 카메라를 향해 회전
/// </summary>
public class StageLabelUI : MonoBehaviour
{
    [Header("공정 인덱스 (StageView와 동일하게 설정)")]
    [Range(0, 4)]
    public int stageIndex = 0;

    [Header("텍스트 위치 (탱크 중심 기준 오프셋)")]
    public Vector3 labelOffset = new Vector3(0f, 1f, 0f);

    [Header("텍스트 스케일 (World Space)")]
    public float labelScale = 0.012f;

    [Header("항상 카메라를 향해 회전")]
    public bool faceCamera = true;

    // 공정 이름 (인덱스 순서)
    private static readonly string[] StageNames =
        { "1차 침전", "폭기조", "2차 침전", "질산화", "소독" };

    private Transform _labelRoot;

    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        CreateLabel();
    }

    void Update()
    {
        if (!faceCamera || _labelRoot == null || Camera.main == null) return;

        // 카메라를 향하되 Y축만 회전 (위아래 기울기 X)
        Vector3 dir = _labelRoot.position - Camera.main.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _labelRoot.rotation = Quaternion.LookRotation(dir);
    }

    // ── 레이블 생성 ───────────────────────────────────────────────

    void CreateLabel()
    {
        if (stageIndex < 0 || stageIndex >= StageNames.Length) return;

        // ── 1. 루트 오브젝트 (위치/회전 담당)
        GameObject root = new GameObject($"StageLabel_{stageIndex}");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = labelOffset;
        _labelRoot = root.transform;

        // ── 2. World Space Canvas (100 × 40)
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRT = root.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(100f, 40f);
        root.transform.localScale = Vector3.one * labelScale;

        // ── 3. 공정 이름 텍스트 (캔버스 전체 영역)
        GameObject go = new GameObject("NameText");
        go.transform.SetParent(root.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = StageNames[stageIndex];
        tmp.fontSize      = 14f;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
