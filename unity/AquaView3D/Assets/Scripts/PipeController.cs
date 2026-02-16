using UnityEngine;

/// <summary>
/// 파이프 색상을 pH 센서 값에 따라 변경합니다.
/// 정상=파랑, 경고=노랑, 위험=빨강으로 시각화합니다.
/// Unity 6 URP 호환: Material 인스턴스 방식 사용.
/// </summary>
public class PipeController : MonoBehaviour
{
    [Tooltip("색상을 변경할 파이프 Renderer 목록")]
    public Renderer[] pipeRenderers;

    private Color targetColor;
    private Material[] pipeMats;

    private readonly Color colorNormal  = new Color(0.13f, 0.59f, 0.95f, 1f);
    private readonly Color colorWarning = new Color(0.96f, 0.62f, 0.04f, 1f);
    private readonly Color colorDanger  = new Color(0.94f, 0.27f, 0.27f, 1f);

    void Start()
    {
        targetColor = colorNormal;

        if (pipeRenderers != null)
        {
            pipeMats = new Material[pipeRenderers.Length];
            for (int i = 0; i < pipeRenderers.Length; i++)
            {
                if (pipeRenderers[i] != null)
                    pipeMats[i] = pipeRenderers[i].material; // 인스턴스 생성
            }
        }

        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate += HandleSensorUpdate;
    }

    void OnDestroy()
    {
        if (SensorDataReceiver.Instance != null)
            SensorDataReceiver.Instance.OnSensorUpdate -= HandleSensorUpdate;

        if (pipeMats != null)
        {
            foreach (var m in pipeMats)
                if (m != null) Destroy(m);
        }
    }

    void Update()
    {
        if (pipeMats == null) return;

        foreach (var mat in pipeMats)
        {
            if (mat == null) continue;
            Color current = mat.GetColor("_BaseColor");
            mat.SetColor("_BaseColor", Color.Lerp(current, targetColor, Time.deltaTime * 3f));
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
