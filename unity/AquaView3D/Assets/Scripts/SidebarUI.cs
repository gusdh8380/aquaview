using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 왼쪽 사이드바 패널을 담당합니다.
/// - 5개 공정 버튼: 클릭 시 해당 시설로 줌인
/// - 각 버튼에 공정 이름 + 상태 색상(점) 표시
/// - 파이프라인 데이터 수신 시 버튼 상태 자동 갱신
///
/// [Inspector 설정]
///   stageViews[5]   : 씬의 StageView 오브젝트 5개 연결
///   stageButtons[5] : 사이드바 Button UI 5개 연결
///   statusDots[5]   : 각 버튼 안의 Image(상태 점) 5개 연결
///   stageLabels[5]  : 각 버튼 안의 TextMeshProUGUI 5개 연결
/// </summary>
public class SidebarUI : MonoBehaviour
{
    [Header("씬 내 StageView 오브젝트 (0=1차침전 ~ 4=소독)")]
    public StageView[] stageViews = new StageView[5];

    [Header("사이드바 버튼 (5개)")]
    public Button[] stageButtons = new Button[5];

    [Header("버튼 내 상태 점 Image (5개)")]
    public Image[] statusDots = new Image[5];

    [Header("버튼 내 공정명 Text (5개)")]
    public TextMeshProUGUI[] stageLabels = new TextMeshProUGUI[5];

    [Header("사이드바 루트 패널")]
    [Tooltip("줌인 중에는 사이드바를 숨기거나 유지 (선택)")]
    public GameObject sidebarRoot;

    // 공정 이름 (인덱스 순서)
    private static readonly string[] StageNames =
        { "1차 침전", "폭기조", "2차 침전", "질산화", "소독" };

    // 서브타이틀
    private static readonly string[] StageSubs =
        { "TSS/BOD 중력 침전", "미생물 유기물 분해", "활성슬러지 분리", "암모니아 제거", "병원균 제거" };

    private static readonly Color ColorNormal  = new Color(0.13f, 0.77f, 0.37f);
    private static readonly Color ColorWarning = new Color(0.96f, 0.62f, 0.04f);
    private static readonly Color ColorDanger  = new Color(0.94f, 0.27f, 0.27f);
    private static readonly Color ColorIdle    = new Color(0.45f, 0.45f, 0.50f);

    void Start()
    {
        // 공정명 초기화
        for (int i = 0; i < stageLabels.Length && i < StageNames.Length; i++)
        {
            if (stageLabels[i] != null)
                stageLabels[i].text = StageNames[i];
        }

        // 상태 점 초기화 (회색)
        foreach (var dot in statusDots)
            if (dot != null) dot.color = ColorIdle;

        // 버튼 이벤트 연결
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int idx = i; // 클로저 캡처
            if (stageButtons[i] != null)
                stageButtons[i].onClick.AddListener(() => OnStageButtonClicked(idx));
        }

        // 파이프라인 이벤트 등록
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnPipelineUpdate += HandlePipelineUpdate;
    }

    void OnDestroy()
    {
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnPipelineUpdate -= HandlePipelineUpdate;
    }

    // ── 버튼 클릭 ─────────────────────────────────────────

    void OnStageButtonClicked(int index)
    {
        if (CameraController.Instance == null) return;
        if (index < 0 || index >= stageViews.Length) return;
        if (stageViews[index] == null) return;

        CameraController.Instance.ZoomIn(stageViews[index]);
    }

    // ── 파이프라인 데이터 수신 → 버튼 상태 갱신 ─────────

    void HandlePipelineUpdate(PipelinePayload payload)
    {
        if (payload?.stages == null) return;

        for (int i = 0; i < statusDots.Length && i < payload.stages.Length; i++)
        {
            if (statusDots[i] == null) continue;

            statusDots[i].color = payload.stages[i].status switch
            {
                "normal"  => ColorNormal,
                "warning" => ColorWarning,
                "danger"  => ColorDanger,
                _         => ColorIdle,
            };
        }
    }

    // ── 사이드바 표시/숨기기 (외부 호출용) ───────────────

    public void SetVisible(bool visible)
    {
        if (sidebarRoot != null)
            sidebarRoot.SetActive(visible);
    }
}
