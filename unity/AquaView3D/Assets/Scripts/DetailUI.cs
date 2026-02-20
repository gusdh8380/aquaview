using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 줌인 시 표시되는 시설 상세 정보 패널을 담당합니다.
///
/// [표시 정보]
///   - 공정 이름 + HRT
///   - 유입수: BOD / TSS / COD / NH3
///   - 유출수: BOD / TSS / COD / NH3
///   - 처리 상태 (정상/경고/위험)
///   - 뒤로가기 버튼 → CameraController.ZoomOut()
///
/// [Inspector 연결 목록]
///   detailPanel        : 상세 패널 루트 GameObject
///   backButton         : 뒤로가기 Button
///   stageNameText      : 공정명 TMP
///   hrtText            : HRT TMP
///   statusText         : 상태 TMP
///   inBODText          : 유입 BOD TMP
///   inTSSText          : 유입 TSS TMP
///   inCODText          : 유입 COD TMP
///   inNH3Text          : 유입 NH3 TMP
///   outBODText         : 유출 BOD TMP
///   outTSSText         : 유출 TSS TMP
///   outCODText         : 유출 COD TMP
///   outNH3Text         : 유출 NH3 TMP
///   removalBODText     : BOD 제거율 TMP
///   removalTSSText     : TSS 제거율 TMP
///   statusBadge        : 상태 색상 Image
/// </summary>
public class DetailUI : MonoBehaviour
{
    [Header("패널 루트")]
    public GameObject detailPanel;

    [Header("뒤로가기 버튼")]
    public Button backButton;

    [Header("공정 정보")]
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI hrtText;
    public TextMeshProUGUI statusText;
    public Image           statusBadge;   // 배경 색상으로 상태 표시

    [Header("유입수 (Influent)")]
    public TextMeshProUGUI inBODText;
    public TextMeshProUGUI inTSSText;
    public TextMeshProUGUI inCODText;
    public TextMeshProUGUI inNH3Text;

    [Header("유출수 (Effluent)")]
    public TextMeshProUGUI outBODText;
    public TextMeshProUGUI outTSSText;
    public TextMeshProUGUI outCODText;
    public TextMeshProUGUI outNH3Text;

    [Header("제거율")]
    public TextMeshProUGUI removalBODText;
    public TextMeshProUGUI removalTSSText;

    // 상태 색상
    private static readonly Color ColorNormal  = new Color(0.13f, 0.77f, 0.37f, 0.92f);
    private static readonly Color ColorWarning = new Color(0.96f, 0.62f, 0.04f, 0.92f);
    private static readonly Color ColorDanger  = new Color(0.94f, 0.27f, 0.27f, 0.92f);
    private static readonly Color ColorIdle    = new Color(0.35f, 0.35f, 0.40f, 0.92f);

    // 공정 이름 (인덱스 순서)
    private static readonly string[] StageNames =
        { "1차 침전", "폭기조", "2차 침전", "질산화", "소독" };

    // 이전 공정의 유출수(= 현 공정 유입수)를 캐시
    private PipelinePayload _lastPayload;
    private StageView       _currentStage;

    void Start()
    {
        // 패널은 처음에 숨김
        if (detailPanel != null)
            detailPanel.SetActive(false);

        // 뒤로가기 버튼
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        // 파이프라인 이벤트 등록 (줌인 상태에서도 데이터 실시간 갱신)
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnPipelineUpdate += HandlePipelineUpdate;
    }

    void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);

        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnPipelineUpdate -= HandlePipelineUpdate;
    }

    // ── 공개 API ─────────────────────────────────────────

    /// <summary>CameraController 줌인 완료 시 호출.</summary>
    public void Show(StageView stage)
    {
        _currentStage = stage;

        if (detailPanel != null)
            detailPanel.SetActive(true);

        // 최신 파이프라인 데이터로 즉시 갱신
        if (_lastPayload != null)
            Refresh(_currentStage.stageIndex, _lastPayload);
    }

    /// <summary>뒤로가기 또는 CameraController.ZoomOut 시 호출.</summary>
    public void Hide()
    {
        _currentStage = null;
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    // ── 파이프라인 데이터 수신 ────────────────────────────

    void HandlePipelineUpdate(PipelinePayload payload)
    {
        _lastPayload = payload;

        // 줌인 중일 때만 갱신
        if (_currentStage != null && detailPanel != null && detailPanel.activeSelf)
            Refresh(_currentStage.stageIndex, payload);
    }

    // ── 데이터 렌더링 ────────────────────────────────────

    void Refresh(int index, PipelinePayload payload)
    {
        if (payload?.stages == null || index >= payload.stages.Length) return;

        StageData stage = payload.stages[index];

        // ── 공정 이름 / HRT
        SetText(stageNameText, StageNames[index]);
        SetText(hrtText, $"HRT {stage.hrt_hours:F1}h  ({stage.hrt_ratio * 100:F0}%)");

        // ── 상태
        Color statusColor = stage.status switch
        {
            "normal"  => ColorNormal,
            "warning" => ColorWarning,
            "danger"  => ColorDanger,
            _         => ColorIdle,
        };
        if (statusBadge != null) statusBadge.color = statusColor;
        SetText(statusText, stage.status switch
        {
            "normal"  => "✓ 정상",
            "warning" => "⚠ 경고",
            "danger"  => "✕ 위험",
            _         => "— 대기",
        });
        if (statusText != null) statusText.color = statusColor;

        // ── 유입수 = 이전 공정 유출수 (0번은 raw_water)
        WaterQualityData influent = index == 0
            ? payload.raw_water
            : payload.stages[index - 1].effluent;

        WaterQualityData effluent = stage.effluent;

        if (influent != null)
        {
            SetText(inBODText, $"BOD : {influent.bod:F1} mg/L");
            SetText(inTSSText, $"TSS : {influent.tss:F1} mg/L");
            SetText(inCODText, $"COD : {influent.cod:F1} mg/L");
            SetText(inNH3Text, $"NH₃ : {influent.ammonia:F2} mg/L");
        }

        if (effluent != null)
        {
            SetText(outBODText, $"BOD : {effluent.bod:F1} mg/L");
            SetText(outTSSText, $"TSS : {effluent.tss:F1} mg/L");
            SetText(outCODText, $"COD : {effluent.cod:F1} mg/L");
            SetText(outNH3Text, $"NH₃ : {effluent.ammonia:F2} mg/L");
        }

        // ── 제거율
        if (influent != null && effluent != null)
        {
            float bodRemoval = influent.bod > 0.01f
                ? (influent.bod - effluent.bod) / influent.bod * 100f : 0f;
            float tssRemoval = influent.tss > 0.01f
                ? (influent.tss - effluent.tss) / influent.tss * 100f : 0f;

            SetText(removalBODText, $"BOD ↓{bodRemoval:F0}%");
            SetText(removalTSSText, $"TSS ↓{tssRemoval:F0}%");

            // 제거율 색상 (높을수록 초록)
            if (removalBODText != null)
                removalBODText.color = bodRemoval >= 50f ? ColorNormal
                                     : bodRemoval >= 20f ? ColorWarning : ColorDanger;
            if (removalTSSText != null)
                removalTSSText.color = tssRemoval >= 50f ? ColorNormal
                                     : tssRemoval >= 20f ? ColorWarning : ColorDanger;
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────

    void OnBackClicked()
    {
        CameraController.Instance?.ZoomOut();
    }

    static void SetText(TextMeshProUGUI tmp, string value)
    {
        if (tmp != null) tmp.text = value;
    }
}
