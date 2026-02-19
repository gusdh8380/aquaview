using UnityEngine;

/// <summary>
/// 탱크 수위 + 물 색상을 제어합니다.
///
/// [RawTank]   — 유량(flow) → 수위, 탁도(turbidity) → 색상 (원수 오염도)
/// [CleanTank] — 파이프라인 결과 BOD/TSS → 수위·색상 (처리 품질)
///
/// tankRole 설정:
///   Raw   → 실시간 센서 데이터 사용
///   Clean → 파이프라인 treated_water 데이터 사용
/// </summary>
public class TankController : MonoBehaviour
{
    public enum TankRole { Raw, Clean }

    [Header("탱크 역할")]
    public TankRole tankRole = TankRole.Raw;

    [Header("수위 설정")]
    [Tooltip("탱크 내부의 물 오브젝트 Transform")]
    public Transform waterTransform;
    public float minWaterScale = 0.05f;
    public float maxWaterScale = 0.92f;

    [Header("유량 범위 (Raw 전용, m³/h)")]
    public float flowMin = 30f;
    public float flowMax = 180f;

    [Header("물 색상")]
    public Renderer waterRenderer;

    // 원수 색 팔레트 (오염도 기반)
    private readonly Color rawColorClean   = new Color(0.30f, 0.65f, 0.90f, 0.82f); // 맑은 파랑
    private readonly Color rawColorWarning = new Color(0.72f, 0.60f, 0.20f, 0.82f); // 탁한 노랑
    private readonly Color rawColorDirty   = new Color(0.48f, 0.32f, 0.12f, 0.82f); // 갈색 (오염)

    // 처리수 색 팔레트 (BOD 기준)
    private readonly Color cleanColorGood    = new Color(0.10f, 0.72f, 0.95f, 0.78f); // 투명 파랑
    private readonly Color cleanColorWarning = new Color(0.96f, 0.62f, 0.04f, 0.78f);
    private readonly Color cleanColorBad     = new Color(0.94f, 0.27f, 0.27f, 0.78f);

    // BOD 방류 기준 (mg/L)
    private const float BOD_NORMAL  = 10f;
    private const float BOD_WARNING = 20f;

    private float  _targetScale;
    private Color  _targetColor;
    private Material _waterMat;

    void Start()
    {
        _targetScale = (minWaterScale + maxWaterScale) * 0.5f;
        _targetColor = tankRole == TankRole.Raw ? rawColorClean : cleanColorGood;

        if (waterRenderer != null)
            _waterMat = waterRenderer.material;   // URP 인스턴스

        var recv = SensorDataReceiver.Instance;
        if (recv != null)
        {
            recv.OnSensorUpdate   += HandleSensorUpdate;
            recv.OnPipelineUpdate += HandlePipelineUpdate;
        }
    }

    void OnDestroy()
    {
        var recv = SensorDataReceiver.Instance;
        if (recv != null)
        {
            recv.OnSensorUpdate   -= HandleSensorUpdate;
            recv.OnPipelineUpdate -= HandlePipelineUpdate;
        }
        if (_waterMat != null) Destroy(_waterMat);
    }

    void Update()
    {
        // 수위 애니메이션
        if (waterTransform != null)
        {
            Vector3 s = waterTransform.localScale;
            s.y = Mathf.Lerp(s.y, _targetScale, Time.deltaTime * 2f);
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

    // ── Raw 탱크: 실시간 센서 → 수위/색상 ────────────────
    void HandleSensorUpdate(SensorPayload payload)
    {
        if (tankRole != TankRole.Raw) return;

        foreach (var s in payload.sensors)
        {
            if (s.sensor == "flow")
            {
                float t = Mathf.InverseLerp(flowMin, flowMax, s.value);
                _targetScale = Mathf.Lerp(minWaterScale, maxWaterScale, t);
            }
            if (s.sensor == "turbidity")
            {
                _targetColor = s.status switch
                {
                    "danger"  => rawColorDirty,
                    "warning" => rawColorWarning,
                    _         => rawColorClean,
                };
            }
        }
    }

    // ── Clean 탱크: 파이프라인 처리수 → 수위/색상 ─────────
    void HandlePipelineUpdate(PipelinePayload payload)
    {
        if (tankRole != TankRole.Clean) return;
        if (payload?.treated_water == null) return;

        var tw = payload.treated_water;

        // BOD 기준으로 수위 결정 (처리 품질이 높을수록 수위 높음)
        float bodRatio = Mathf.InverseLerp(50f, BOD_NORMAL, tw.bod); // 낮을수록 좋음
        _targetScale   = Mathf.Lerp(minWaterScale + 0.1f, maxWaterScale, bodRatio);

        // BOD 기준 색상
        _targetColor = tw.bod switch
        {
            var b when b <= BOD_NORMAL  => cleanColorGood,
            var b when b <= BOD_WARNING => cleanColorWarning,
            _                           => cleanColorBad,
        };
    }
}
