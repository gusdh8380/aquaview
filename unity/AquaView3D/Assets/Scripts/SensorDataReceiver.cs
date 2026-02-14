using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// React iframe에서 postMessage로 전송된 센서 데이터를 수신하는 싱글턴.
/// WebGL 빌드 시 jslib 플러그인과 연동됩니다.
/// </summary>
public class SensorDataReceiver : MonoBehaviour
{
    public static SensorDataReceiver Instance { get; private set; }

    public event Action<SensorPayload> OnSensorUpdate;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// JavaScript jslib에서 호출되는 콜백.
    /// SendMessage("SensorDataReceiver", "ReceiveSensorData", jsonString) 형태.
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
            Debug.LogWarning($"[SensorDataReceiver] JSON parse error: {e.Message}");
        }
    }
}

/* ── JSON 매핑 모델 ── */

[Serializable]
public class SensorPayload
{
    public SensorItem[] sensors;
}

[Serializable]
public class SensorItem
{
    public string sensor;   // ph, turbidity, flow, temp
    public float value;
    public string unit;
    public string status;   // normal, warning, danger
}
