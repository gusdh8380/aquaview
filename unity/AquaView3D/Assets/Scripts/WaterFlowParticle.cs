using UnityEngine;

/// <summary>
/// 파이프를 따라 흐르는 물 파티클 시스템.
/// 유량(flow) 값에 따라 파티클 속도/방출량이 변하고,
/// 파이프라인 수질 상태(BOD 등)에 따라 파티클 색상이 변합니다.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WaterFlowParticle : MonoBehaviour
{
    [Header("유량 범위 (m³/h)")]
    public float flowMin = 30f;
    public float flowMax = 180f;

    [Header("파티클 속도 범위")]
    public float minSpeed    = 1f;
    public float maxSpeed    = 8f;

    [Header("파티클 방출량 범위")]
    public float minEmission = 10f;
    public float maxEmission = 80f;

    [Header("수질 상태 색상")]
    // 파이프라인 처리 결과 기반 색상 (BOD/TSS 상태)
    private readonly Color colorClean   = new Color(0.13f, 0.70f, 0.95f, 0.85f); // 맑은 파랑
    private readonly Color colorWarning = new Color(0.96f, 0.75f, 0.10f, 0.85f); // 노랑
    private readonly Color colorDirty   = new Color(0.55f, 0.35f, 0.15f, 0.85f); // 갈색 (오염)

    private ParticleSystem _ps;
    private ParticleSystem.MainModule _main;
    private ParticleSystem.EmissionModule _emission;

    private Color _targetColor;
    private float _targetSpeed;
    private float _targetEmission;

    void Awake()
    {
        _ps       = GetComponent<ParticleSystem>();
        _main     = _ps.main;
        _emission = _ps.emission;

        _targetColor    = colorClean;
        _targetSpeed    = minSpeed;
        _targetEmission = minEmission;
    }

    void Start()
    {
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate += HandleSensorUpdate;
    }

    void OnDestroy()
    {
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate -= HandleSensorUpdate;
    }

    void Update()
    {
        // 속도 보간
        float curSpeed = _main.startSpeed.constant;
        _main.startSpeed = Mathf.Lerp(curSpeed, _targetSpeed, Time.deltaTime * 3f);

        // 방출량 보간
        float curRate = _emission.rateOverTime.constant;
        _emission.rateOverTime = Mathf.Lerp(curRate, _targetEmission, Time.deltaTime * 3f);

        // 색상 보간
        Color curColor = _main.startColor.color;
        _main.startColor = Color.Lerp(curColor, _targetColor, Time.deltaTime * 2f);
    }

    void HandleSensorUpdate(SensorPayload payload)
    {
        foreach (var s in payload.sensors)
        {
            // 유량 → 속도/방출량
            if (s.sensor == "flow")
            {
                float t = Mathf.InverseLerp(flowMin, flowMax, s.value);
                _targetSpeed    = Mathf.Lerp(minSpeed,    maxSpeed,    t);
                _targetEmission = Mathf.Lerp(minEmission, maxEmission, t);
            }

            // 탁도 → 파티클 색상 (원수 오염도 표현)
            if (s.sensor == "turbidity")
            {
                _targetColor = s.status switch
                {
                    "danger"  => colorDirty,
                    "warning" => colorWarning,
                    _         => colorClean,
                };
            }
        }
    }

    // ── 파이프라인 결과로 색상 재정의 (Unity3DView에서 호출 가능) ──
    public void SetPipelineStatus(string status)
    {
        _targetColor = status switch
        {
            "danger"  => colorDirty,
            "warning" => colorWarning,
            _         => colorClean,
        };
    }
}
