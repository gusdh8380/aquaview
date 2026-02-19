using UnityEngine;

/// <summary>
/// 유량(flow) 센서값에 따라 펌프를 회전시키고
/// 위험 상태일 때 경고 색상으로 변경합니다.
/// </summary>
public class PumpAnimator : MonoBehaviour
{
    [Header("회전 설정")]
    [Tooltip("분당 최소 회전속도 (유량 최솟값일 때)")]
    public float minRotationSpeed = 60f;    // deg/sec

    [Tooltip("분당 최대 회전속도 (유량 최댓값일 때)")]
    public float maxRotationSpeed = 360f;

    [Header("유량 범위 (m³/h)")]
    public float flowMin = 30f;
    public float flowMax = 180f;

    [Header("색상")]
    public Renderer pumpRenderer;

    private readonly Color colorNormal  = new Color(0.55f, 0.55f, 0.60f, 1f); // 금속 회색
    private readonly Color colorWarning = new Color(0.96f, 0.62f, 0.04f, 1f);
    private readonly Color colorDanger  = new Color(0.94f, 0.27f, 0.27f, 1f);

    private float _currentSpeed;
    private float _targetSpeed;
    private Color _targetColor;
    private Material _pumpMat;

    void Start()
    {
        _currentSpeed = minRotationSpeed;
        _targetSpeed  = minRotationSpeed;
        _targetColor  = colorNormal;

        if (pumpRenderer != null)
            _pumpMat = pumpRenderer.material;

        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate += HandleSensorUpdate;
    }

    void OnDestroy()
    {
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate -= HandleSensorUpdate;

        if (_pumpMat != null) Destroy(_pumpMat);
    }

    void Update()
    {
        // 회전속도 부드럽게 전환
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime * 2f);

        // Y축 회전
        transform.Rotate(Vector3.up, _currentSpeed * Time.deltaTime, Space.Self);

        // 색상 전환
        if (_pumpMat != null)
        {
            Color current = _pumpMat.GetColor("_BaseColor");
            _pumpMat.SetColor("_BaseColor",
                Color.Lerp(current, _targetColor, Time.deltaTime * 4f));
        }
    }

    void HandleSensorUpdate(SensorPayload payload)
    {
        foreach (var s in payload.sensors)
        {
            if (s.sensor != "flow") continue;

            float t = Mathf.InverseLerp(flowMin, flowMax, s.value);
            _targetSpeed = Mathf.Lerp(minRotationSpeed, maxRotationSpeed, t);

            _targetColor = s.status switch
            {
                "danger"  => colorDanger,
                "warning" => colorWarning,
                _         => colorNormal,
            };
        }
    }
}
