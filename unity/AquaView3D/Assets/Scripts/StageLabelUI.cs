using UnityEngine;
using TMPro;

/// <summary>
/// 탱크(StageView) 위에 공정명 + 기능 설명 텍스트를 월드 공간에 표시합니다.
///
/// [사용 방법]
///   StageView GameObject에 이 컴포넌트를 추가하면 자동으로
///   World Space Canvas + TextMeshPro 텍스트를 생성합니다.
///   별도 Prefab/Inspector 연결 불필요.
///
/// [표시 내용]
///   윗줄: 공정 이름  (예: "폭기조")
///   아랫줄: 기능 설명 (예: "미생물 유기물 분해")
///
/// [Inspector 설정]
///   stageIndex     : 공정 인덱스 (0~4), StageView와 동일하게 설정
///   labelOffset    : 탱크 중심으로부터의 텍스트 위치 오프셋 (기본 0, 3.5, 0)
///   labelScale     : 텍스트 크기 배율 (기본 0.012)
///   faceCamera     : true면 항상 카메라를 향해 회전
/// </summary>
public class StageLabelUI : MonoBehaviour
{
    [Header("공정 인덱스 (StageView와 동일하게 설정)")]
    [Range(0, 4)]
    public int stageIndex = 0;

    [Header("텍스트 위치 (탱크 중심 기준 오프셋)")]
    public Vector3 labelOffset = new Vector3(0f, 3.5f, 0f);

    [Header("텍스트 스케일 (World Space)")]
    public float labelScale = 0.012f;

    [Header("항상 카메라를 향해 회전")]
    public bool faceCamera = true;

    // 공정 이름 / 기능 설명 (인덱스 순서)
    private static readonly string[] StageNames =
        { "1차 침전", "폭기조", "2차 침전", "질산화", "소독" };

    private static readonly string[] StageDescs =
        { "TSS/BOD 중력 침전", "미생물 유기물 분해", "활성슬러지 분리", "암모니아 제거", "병원균 제거" };

    // 배경 색상 (공정별 구분)
    private static readonly Color[] StageBgColors =
    {
        new Color(0.08f, 0.38f, 0.60f, 0.75f), // 1차 침전 — 짙은 파랑
        new Color(0.10f, 0.55f, 0.32f, 0.75f), // 폭기조   — 초록
        new Color(0.12f, 0.35f, 0.65f, 0.75f), // 2차 침전 — 파랑
        new Color(0.55f, 0.30f, 0.05f, 0.75f), // 질산화   — 갈색
        new Color(0.50f, 0.10f, 0.45f, 0.75f), // 소독     — 보라
    };

    private Transform _labelRoot;

    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        CreateLabel();
    }

    void Update()
    {
        if (!faceCamera || _labelRoot == null || Camera.main == null) return;

        // 카메라를 향하되 수직 축(Y)만 회전 (위아래 기울기 X)
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

        // ── 2. World Space Canvas
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRT = root.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(300f, 80f);
        root.transform.localScale = Vector3.one * labelScale;

        // ── 3. 배경 패널 (반투명)
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);

        UnityEngine.UI.Image bgImage = bg.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = stageIndex < StageBgColors.Length
            ? StageBgColors[stageIndex]
            : new Color(0.1f, 0.1f, 0.1f, 0.7f);

        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin        = Vector2.zero;
        bgRT.anchorMax        = Vector2.one;
        bgRT.offsetMin        = Vector2.zero;
        bgRT.offsetMax        = Vector2.zero;

        // ── 4. 공정 이름 텍스트 (위)
        CreateText(root.transform, "NameText",
            text        : StageNames[stageIndex],
            anchorMin   : new Vector2(0f, 0.45f),
            anchorMax   : new Vector2(1f, 1.00f),
            fontSize    : 36f,
            bold        : true,
            color       : Color.white);

        // ── 5. 기능 설명 텍스트 (아래)
        CreateText(root.transform, "DescText",
            text        : StageDescs[stageIndex],
            anchorMin   : new Vector2(0f, 0.00f),
            anchorMax   : new Vector2(1f, 0.50f),
            fontSize    : 22f,
            bold        : false,
            color       : new Color(0.85f, 0.95f, 1.00f));
    }

    // ── 텍스트 오브젝트 헬퍼 ──────────────────────────────────────

    static void CreateText(Transform parent, string name,
        string text, Vector2 anchorMin, Vector2 anchorMax,
        float fontSize, bool bold, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;

        // 텍스트가 배경 위에 보이도록 raycast 끄기
        tmp.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.offsetMin   = new Vector2(4f, 2f);
        rt.offsetMax   = new Vector2(-4f, -2f);
    }
}
