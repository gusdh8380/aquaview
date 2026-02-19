using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 UI를 담당합니다:
/// - 카메라 리셋 버튼
/// - 센서 레이블 토글
/// - 5단계 공정 상태바 (파이프라인 결과 기반)
/// - 전체 처리 상태 텍스트
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("버튼")]
    public Button resetCameraButton;
    public Button toggleLabelsButton;

    [Header("카메라")]
    public OrbitCamera orbitCamera;

    [Header("센서 레이블 루트")]
    [Tooltip("씬 내 모든 SensorLabel 오브젝트의 부모 또는 배열")]
    public GameObject[] sensorLabelObjects;

    [Header("공정 상태 UI (5단계)")]
    public Image[] stageStatusDots;          // 5개 Image — 색상으로 상태 표시
    public TextMeshProUGUI[] stageNameTexts; // 5개 Text — 공정명
    public TextMeshProUGUI overallStatusText;

    [Header("처리수 수질 요약")]
    public TextMeshProUGUI treatedBODText;
    public TextMeshProUGUI treatedTSSText;
    public TextMeshProUGUI treatedNH3Text;

    // 공정명 (파이프라인 순서)
    private static readonly string[] StageNames =
    {
        "1차침전", "폭기조", "2차침전", "질산화", "소독"
    };

    private static readonly Color ColorNormal  = new Color(0.13f, 0.77f, 0.37f);
    private static readonly Color ColorWarning = new Color(0.96f, 0.62f, 0.04f);
    private static readonly Color ColorDanger  = new Color(0.94f, 0.27f, 0.27f);
    private static readonly Color ColorUnknown = new Color(0.40f, 0.40f, 0.45f);

    private bool _labelsVisible = true;

    void Start()
    {
        // 버튼 이벤트 연결
        resetCameraButton?.onClick.AddListener(OnResetCamera);
        toggleLabelsButton?.onClick.AddListener(OnToggleLabels);

        // 공정명 초기화
        if (stageNameTexts != null)
        {
            for (int i = 0; i < stageNameTexts.Length && i < StageNames.Length; i++)
            {
                if (stageNameTexts[i] != null)
                    stageNameTexts[i].text = StageNames[i];
            }
        }

        // 상태 점 초기화
        SetAllDotsUnknown();

        // 파이프라인 데이터 수신 등록
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnPipelineUpdate += HandlePipelineUpdate;
    }

    void OnDestroy()
    {
        resetCameraButton?.onClick.RemoveListener(OnResetCamera);
        toggleLabelsButton?.onClick.RemoveListener(OnToggleLabels);

        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnPipelineUpdate -= HandlePipelineUpdate;
    }

    // ── 버튼 핸들러 ──────────────────────────────────────

    void OnResetCamera()
    {
        orbitCamera?.ResetCamera();
    }

    void OnToggleLabels()
    {
        _labelsVisible = !_labelsVisible;
        if (sensorLabelObjects == null) return;
        foreach (var obj in sensorLabelObjects)
            if (obj != null) obj.SetActive(_labelsVisible);

        if (toggleLabelsButton != null)
        {
            var text = toggleLabelsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = _labelsVisible ? "레이블 숨기기" : "레이블 표시";
        }
    }

    // ── 파이프라인 상태 업데이트 ──────────────────────────

    void HandlePipelineUpdate(PipelinePayload payload)
    {
        if (payload == null) return;

        // 1. 5단계 공정 상태 점 업데이트
        if (stageStatusDots != null && payload.stages != null)
        {
            for (int i = 0; i < stageStatusDots.Length && i < payload.stages.Length; i++)
            {
                if (stageStatusDots[i] == null) continue;
                stageStatusDots[i].color = StatusToColor(payload.stages[i].status);
            }
        }

        // 2. 전체 처리 상태 텍스트
        if (overallStatusText != null)
        {
            overallStatusText.color = StatusToColor(payload.overall_status);
            overallStatusText.text = payload.overall_status switch
            {
                "normal"  => "✓ 방류 기준 적합",
                "warning" => "⚠ 일부 기준 초과",
                "danger"  => "✕ 기준 초과 — 재처리",
                _         => "— 데이터 수신 중"
            };
        }

        // 3. 처리수 수질 요약
        var tw = payload.treated_water;
        if (tw != null)
        {
            if (treatedBODText != null)
                treatedBODText.text = $"BOD {tw.bod:F1} mg/L";
            if (treatedTSSText != null)
                treatedTSSText.text = $"TSS {tw.tss:F1} mg/L";
            if (treatedNH3Text != null)
                treatedNH3Text.text = $"NH₃ {tw.ammonia:F2} mg/L";
        }
    }

    // ── 헬퍼 ──────────────────────────────────────────────

    Color StatusToColor(string status) => status switch
    {
        "normal"  => ColorNormal,
        "warning" => ColorWarning,
        "danger"  => ColorDanger,
        _         => ColorUnknown,
    };

    void SetAllDotsUnknown()
    {
        if (stageStatusDots == null) return;
        foreach (var dot in stageStatusDots)
            if (dot != null) dot.color = ColorUnknown;
    }
}
