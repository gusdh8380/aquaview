using UnityEngine;

/// <summary>
/// 수처리 5단계 중 하나의 시설(탱크)을 담당합니다.
/// - stageIndex에 맞는 파이프라인 데이터를 수신
/// - 수위·색상을 BOD/TSS/status 기반으로 실시간 갱신
/// - CameraController가 줌인 타겟으로 사용하는 위치 제공
/// </summary>
public class StageView : MonoBehaviour
{
    [Header("공정 인덱스 (0=1차침전 ~ 4=소독)")]
    [Range(0, 4)]
    public int stageIndex = 0;

    [Header("공정 이름")]
    public string stageNameKo = "1차 침전";

    [Header("물 오브젝트")]
    [Tooltip("탱크 내부 물 Cube의 Transform")]
    public Transform waterTransform;
    public Renderer  waterRenderer;

    [Header("수위 범위")]
    public float minWaterScale = 0.05f;
    public float maxWaterScale = 0.90f;

    // 상태별 물 색상
    private static readonly Color ColorNormal  = new Color(0.10f, 0.72f, 0.95f, 0.80f); // 맑은 파랑
    private static readonly Color ColorWarning = new Color(0.96f, 0.62f, 0.04f, 0.80f); // 경고 노랑
    private static readonly Color ColorDanger  = new Color(0.94f, 0.27f, 0.27f, 0.80f); // 위험 빨강
    private static readonly Color ColorIdle    = new Color(0.30f, 0.55f, 0.70f, 0.60f); // 데이터 없음

    // 현재 이 스테이지의 캐시된 데이터
    public StageData CurrentStageData { get; private set; }

    private float    _targetScale;
    private Color    _targetColor;
    private Material _waterMat;

    void Start()
    {
        _targetScale = (minWaterScale + maxWaterScale) * 0.5f;
        _targetColor = ColorIdle;

        if (waterRenderer != null)
            _waterMat = waterRenderer.material; // URP 인스턴스

        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnPipelineUpdate += HandlePipelineUpdate;
    }

    void OnDestroy()
    {
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnPipelineUpdate -= HandlePipelineUpdate;

        if (_waterMat != null) Destroy(_waterMat);
    }

    void Update()
    {
        // 수위 애니메이션 (부드러운 Lerp)
        if (waterTransform != null)
        {
            Vector3 s = waterTransform.localScale;
            s.y = Mathf.Lerp(s.y, _targetScale, Time.deltaTime * 2.5f);
            waterTransform.localScale = s;
        }

        // 색상 애니메이션
        if (_waterMat != null)
        {
            Color cur = _waterMat.GetColor("_BaseColor");
            _waterMat.SetColor("_BaseColor",
                Color.Lerp(cur, _targetColor, Time.deltaTime * 3f));
        }
    }

    void HandlePipelineUpdate(PipelinePayload payload)
    {
        if (payload?.stages == null) return;
        if (stageIndex >= payload.stages.Length) return;

        StageData stage = payload.stages[stageIndex];
        CurrentStageData = stage;

        // 상태에 따른 색상
        _targetColor = stage.status switch
        {
            "normal"  => ColorNormal,
            "warning" => ColorWarning,
            "danger"  => ColorDanger,
            _         => ColorIdle,
        };

        // BOD 기준 수위 (낮을수록 처리 잘 된 것 → 수위 높게 표시)
        // 1차 침전 직후 BOD는 여전히 높을 수 있으므로 상대값으로 정규화
        if (stage.effluent != null)
        {
            // BOD 0~200 범위를 수위로 역매핑 (낮을수록 clean → 수위 높음)
            float normalizedBOD = Mathf.InverseLerp(200f, 0f, stage.effluent.bod);
            _targetScale = Mathf.Lerp(minWaterScale + 0.05f, maxWaterScale, normalizedBOD);
        }
    }

    /// <summary>
    /// CameraController가 이 시설로 줌인할 때 사용할 타겟 위치 반환.
    /// </summary>
    public Vector3 GetFocusPosition() => transform.position;
}
