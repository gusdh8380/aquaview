using UnityEngine;

/// <summary>
/// 탱크 내부 수위를 유량(flow) 센서 값에 따라 애니메이션합니다.
/// Water 오브젝트의 Y 스케일을 조절하여 수위를 표현합니다.
/// </summary>
public class TankController : MonoBehaviour
{
    [Header("수위 설정")]
    [Tooltip("탱크 내부의 물 오브젝트")]
    public Transform waterTransform;

    [Tooltip("최소 수위 Y 스케일")]
    public float minWaterScale = 0.1f;

    [Tooltip("최대 수위 Y 스케일")]
    public float maxWaterScale = 0.9f;

    [Header("유량 범위 (m³/h)")]
    public float flowMin = 30f;
    public float flowMax = 180f;

    [Header("색상 설정")]
    public Renderer waterRenderer;

    private float targetScale;
    private Color targetColor;
    private MaterialPropertyBlock propBlock;

    private readonly Color colorNormal  = new Color(0.13f, 0.59f, 0.95f, 0.8f);  // 파랑
    private readonly Color colorWarning = new Color(0.96f, 0.62f, 0.04f, 0.8f);  // 주황
    private readonly Color colorDanger  = new Color(0.94f, 0.27f, 0.27f, 0.8f);  // 빨강

    void Start()
    {
        propBlock = new MaterialPropertyBlock();
        targetScale = maxWaterScale * 0.5f;
        targetColor = colorNormal;

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
        if (waterTransform == null) return;

        // 부드러운 수위 변화
        Vector3 scale = waterTransform.localScale;
        scale.y = Mathf.Lerp(scale.y, targetScale, Time.deltaTime * 2f);
        waterTransform.localScale = scale;

        // 부드러운 색상 변화
        if (waterRenderer != null)
        {
            waterRenderer.GetPropertyBlock(propBlock);
            Color current = propBlock.GetColor("_BaseColor");
            if (current.a == 0) current = colorNormal;
            Color next = Color.Lerp(current, targetColor, Time.deltaTime * 3f);
            propBlock.SetColor("_BaseColor", next);
            waterRenderer.SetPropertyBlock(propBlock);
        }
    }

    void HandleSensorUpdate(SensorPayload payload)
    {
        foreach (var s in payload.sensors)
        {
            if (s.sensor == "flow")
            {
                float t = Mathf.InverseLerp(flowMin, flowMax, s.value);
                targetScale = Mathf.Lerp(minWaterScale, maxWaterScale, t);
            }

            if (s.sensor == "turbidity")
            {
                targetColor = s.status switch
                {
                    "warning" => colorWarning,
                    "danger"  => colorDanger,
                    _         => colorNormal
                };
            }
        }
    }
}
