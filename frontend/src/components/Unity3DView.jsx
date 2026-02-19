import { useRef, useEffect } from "react";

/**
 * Unity WebGL 3D 뷰 컴포넌트
 *
 * React → Unity 브릿지 (postMessage):
 *   1. SENSOR_UPDATE   — 실시간 센서 (pH, 탁도, 유량, 수온)
 *   2. PIPELINE_UPDATE — 파이프라인 계산 결과 (BOD, TSS, 5단계 상태)
 *
 * Unity 수신:
 *   SendMessage("SensorDataReceiver", "ReceiveSensorData",  json)
 *   SendMessage("SensorDataReceiver", "ReceivePipelineData", json)
 */
export default function Unity3DView({ sensors, pipelineResult }) {
  const iframeRef = useRef(null);

  // 1. 실시간 센서 데이터 전송
  useEffect(() => {
    if (!sensors || sensors.length === 0) return;
    const iframe = iframeRef.current;
    if (!iframe?.contentWindow) return;

    iframe.contentWindow.postMessage(
      { type: "SENSOR_UPDATE", sensors },
      "*"
    );
  }, [sensors]);

  // 2. 파이프라인 결과 전송 (HRT 슬라이더 변경 시)
  useEffect(() => {
    if (!pipelineResult) return;
    const iframe = iframeRef.current;
    if (!iframe?.contentWindow) return;

    iframe.contentWindow.postMessage(
      { type: "PIPELINE_UPDATE", pipeline: pipelineResult },
      "*"
    );
  }, [pipelineResult]);

  const isLoading = !sensors || sensors.length === 0;

  return (
    <div className="unity-view-container">
      <h2 className="section-title">🏭 3D 공정 뷰</h2>
      <div className="unity-frame-wrapper">
        <iframe
          ref={iframeRef}
          src="/unity/AquaView3D/index.html"
          title="AquaView 3D"
          className="unity-iframe"
          allow="autoplay; fullscreen"
        />
        {isLoading && (
          <div className="unity-overlay-hint">데이터 로딩 중...</div>
        )}
      </div>
    </div>
  );
}
