using UnityEngine;

/// <summary>
/// 파이프 색상을 pH 센서 값에 따라 변경합니다.
/// 정상=파랑, 경고=노랑, 위험=빨강으로 시각화합니다.
/// </summary>
public class PipeController : MonoBehaviour
{
    [Tooltip("색상을 변경할 파이프 Renderer 목록")]
    public Renderer[] pipeRenderers;

    private Color targetColor;
    private MaterialPropertyBlock propBlock;

    private readonly Color colorNormal  = new Color(0.13f, 0.59f, 0.95f, 1f);
    private readonly Color colorWarning = new Color(0.96f, 0.62f, 0.04f, 1f);
    private readonly Color colorDanger  = new Color(0.94f, 0.27f, 0.27f, 1f);

    void Start()
    {
        propBlock = new MaterialPropertyBlock();
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
        if (pipeRenderers == null) return;

        foreach (var r in pipeRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            Color current = propBlock.GetColor("_BaseColor");
            if (current.a == 0) current = colorNormal;
            Color next = Color.Lerp(current, targetColor, Time.deltaTime * 3f);
            propBlock.SetColor("_BaseColor", next);
            r.SetPropertyBlock(propBlock);
        }
    }

    void HandleSensorUpdate(SensorPayload payload)
    {
        foreach (var s in payload.sensors)
        {
            if (s.sensor == "ph")
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
