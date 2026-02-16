using UnityEngine;

/// <summary>
/// 탱크 내부 수위를 유량(flow) 센서 값에 따라 애니메이션합니다.
/// Water 오브젝트의 Y 스케일을 조절하여 수위를 표현합니다.
/// Unity 6 URP 호환: Material 인스턴스 방식 사용.
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
    private Material waterMat;

    private readonly Color colorNormal  = new Color(0.13f, 0.59f, 0.95f, 0.8f);
    private readonly Color colorWarning = new Color(0.96f, 0.62f, 0.04f, 0.8f);
    private readonly Color colorDanger  = new Color(0.94f, 0.27f, 0.27f, 0.8f);

    void Start()
    {
        targetScale = maxWaterScale * 0.5f;
        targetColor = colorNormal;

        if (waterRenderer != null)
            waterMat = waterRenderer.material; // 인스턴스 생성

        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate += HandleSensorUpdate;
    }

    void OnDestroy()
    {
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate -= HandleSensorUpdate;

        if (waterMat != null)
            Destroy(waterMat);
    }

    void Update()
    {
        if (waterTransform == null) return;

        Vector3 scale = waterTransform.localScale;
        scale.y = Mathf.Lerp(scale.y, targetScale, Time.deltaTime * 2f);
        waterTransform.localScale = scale;

        if (waterMat != null)
        {
            Color current = waterMat.GetColor("_BaseColor");
            waterMat.SetColor("_BaseColor", Color.Lerp(current, targetColor, Time.deltaTime * 3f));
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
