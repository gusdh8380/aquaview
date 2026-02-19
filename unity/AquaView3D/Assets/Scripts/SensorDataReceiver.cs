using System;
using UnityEngine;

/// <summary>
/// React iframe에서 postMessage로 전송된 데이터를 수신하는 싱글턴.
/// 두 가지 메시지 타입을 처리합니다:
///   1. SENSOR_UPDATE  → 실시간 센서 (pH, 탁도, 유량, 수온)
///   2. PIPELINE_UPDATE → 파이프라인 수질 계산 결과 (BOD, TSS, NH3 등 5단계)
/// WebGL 빌드 시 index.html의 SendMessage 브릿지와 연동됩니다.
/// </summary>
public class SensorDataReceiver : MonoBehaviour
{
    public static SensorDataReceiver Instance { get; private set; }

    // 실시간 센서 이벤트 (pH, 탁도, 유량, 수온)
    public event Action<SensorPayload> OnSensorUpdate;

    // 파이프라인 계산 결과 이벤트 (BOD, TSS, NH3, 5단계 상태)
    public event Action<PipelinePayload> OnPipelineUpdate;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// JavaScript SendMessage("SensorDataReceiver", "ReceiveSensorData", json) 로 호출.
    /// 실시간 센서 데이터 수신.
    /// </summary>
    public void ReceiveSensorData(string json)
    {
        try
        {
            SensorPayload payload = JsonUtility.FromJson<SensorPayload>(json);
            OnSensorUpdate?.Invoke(payload);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SensorDataReceiver] SensorData JSON parse error: {e.Message}");
        }
    }

    /// <summary>
    /// JavaScript SendMessage("SensorDataReceiver", "ReceivePipelineData", json) 로 호출.
    /// 파이프라인 계산 결과 수신 (HRT 슬라이더 변경 시).
    /// </summary>
    public void ReceivePipelineData(string json)
    {
        try
        {
            PipelinePayload payload = JsonUtility.FromJson<PipelinePayload>(json);
            OnPipelineUpdate?.Invoke(payload);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SensorDataReceiver] PipelineData JSON parse error: {e.Message}");
        }
    }
}

// ═══════════════════════════════════════════════════════
//  실시간 센서 모델
// ═══════════════════════════════════════════════════════

[Serializable]
public class SensorPayload
{
    public SensorItem[] sensors;
}

[Serializable]
public class SensorItem
{
    public string sensor;   // ph, turbidity, flow, temp
    public float  value;
    public string unit;
    public string status;   // normal, warning, danger
}

// ═══════════════════════════════════════════════════════
//  파이프라인 수질 계산 결과 모델
//  (FastAPI /api/pipeline 응답 구조와 일치)
// ═══════════════════════════════════════════════════════

[Serializable]
public class PipelinePayload
{
    public WaterQualityData raw_water;
    public StageData[]      stages;
    public WaterQualityData treated_water;
    public string           overall_status;  // normal, warning, danger
}

[Serializable]
public class StageData
{
    public string           stage;           // primary_settling, aeration, ...
    public string           stage_name_ko;   // "1차 침전" 등
    public float            hrt_ratio;
    public float            hrt_hours;
    public WaterQualityData effluent;
    public string           status;          // normal, warning, danger
}

[Serializable]
public class WaterQualityData
{
    public float bod;
    public float tss;
    public float cod;
    public float ammonia;
    public float turbidity;
    public float ph;
    public float do_level;
    public float coliform;
}
