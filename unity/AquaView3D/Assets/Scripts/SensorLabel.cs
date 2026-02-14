using UnityEngine;
using TMPro;

/// <summary>
/// 3D 공간에 센서 값을 텍스트로 표시합니다.
/// TextMeshPro - Text (World Space) 컴포넌트가 필요합니다.
/// </summary>
public class SensorLabel : MonoBehaviour
{
    [Tooltip("표시할 센서 타입 (ph, turbidity, flow, temp)")]
    public string sensorType = "ph";

    private TextMeshPro label;

    private readonly Color colorNormal  = new Color(0.13f, 0.77f, 0.37f);
    private readonly Color colorWarning = new Color(0.96f, 0.62f, 0.04f);
    private readonly Color colorDanger  = new Color(0.94f, 0.27f, 0.27f);

    void Start()
    {
        label = GetComponent<TextMeshPro>();
        if (label == null)
            label = gameObject.AddComponent<TextMeshPro>();

        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate += HandleSensorUpdate;
    }

    void OnDestroy()
    {
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate -= HandleSensorUpdate;
    }

    void HandleSensorUpdate(SensorPayload payload)
    {
        foreach (var s in payload.sensors)
        {
            if (s.sensor != sensorType) continue;

            label.text = $"{s.value:F1} {s.unit}";
            label.color = s.status switch
            {
                "warning" => colorWarning,
                "danger"  => colorDanger,
                _         => colorNormal
            };
        }
    }
}
