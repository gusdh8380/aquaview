import { useState, useCallback, useEffect } from "react";
import usePolling from "./hooks/usePolling";
import { fetchSensors, fetchAlerts, fetchPipeline, updatePipelineParams } from "./api/client";
import SensorCard from "./components/SensorCard";
import HistoryChart from "./components/HistoryChart";
import AlertPanel from "./components/AlertPanel";
import Unity3DView from "./components/Unity3DView";
import ProcessFlowDiagram from "./components/ProcessFlowDiagram";
import HRTControls from "./components/HRTControls";
import WaterQualityComparison from "./components/WaterQualityComparison";
import "./App.css";

// Default HRT ratios (1.0 = design value)
const DEFAULT_HRT_RATIOS = {
  primary_settling: 1.0,
  aeration: 1.0,
  secondary_settling: 1.0,
  nitrification: 1.0,
  disinfection: 1.0,
};

const STATUS_COLOR = { normal: "#22c55e", warning: "#f59e0b", danger: "#ef4444" };
const STATUS_LABEL = { normal: "정상 가동", warning: "주의 필요", danger: "위험 감지" };

function App() {
  const [selectedSensor, setSelectedSensor] = useState("ph");
  const [selectedStage, setSelectedStage] = useState("aeration");
  const [hrtRatios, setHrtRatios] = useState(DEFAULT_HRT_RATIOS);
  const [pipelineResult, setPipelineResult] = useState(null);
  const [pipelineLoading, setPipelineLoading] = useState(true);

  const { data: sensors, loading: sensorsLoading } = usePolling(fetchSensors, 3000);
  const { data: alerts } = usePolling(fetchAlerts, 3000);

  // Pipeline polling — 10초마다 갱신 (Unity 초기화 후에도 데이터 수신 보장)
  const { data: polledPipeline } = usePolling(fetchPipeline, 10000);
  useEffect(() => {
    if (polledPipeline) {
      setPipelineResult(polledPipeline);
      setPipelineLoading(false);
    }
  }, [polledPipeline]);

  // Called by HRTControls when sliders change
  const handleHRTChange = useCallback(async (newRatios, shouldCallAPI) => {
    setHrtRatios(newRatios);
    if (!shouldCallAPI) return;

    try {
      const params = Object.entries(newRatios).map(([stage, hrt_ratio]) => ({
        stage,
        hrt_ratio,
      }));
      const result = await updatePipelineParams(params);
      setPipelineResult(result);
    } catch (err) {
      console.error("Pipeline update failed:", err);
    }
  }, []);

  // 전체 시스템 상태
  const systemStatus = sensors?.some((s) => s.status === "danger")
    ? "danger"
    : sensors?.some((s) => s.status === "warning")
    ? "warning"
    : "normal";

  return (
    <div className="app">
      <header className="app-header">
        <div className="app-header-title">
          <h1>💧 AquaView</h1>
          <p>수처리 공정 모니터링 시스템</p>
        </div>
        {sensors && (
          <div
            className="system-status-badge"
            style={{
              color: STATUS_COLOR[systemStatus],
              borderColor: `${STATUS_COLOR[systemStatus]}66`,
              background: `${STATUS_COLOR[systemStatus]}11`,
            }}
          >
            <span
              className="system-status-dot"
              style={{ background: STATUS_COLOR[systemStatus] }}
            />
            {STATUS_LABEL[systemStatus]}
          </div>
        )}
      </header>

      <main className="app-main">
        {/* ── Sensor Cards ── */}
        <section className="sensor-cards">
          {sensorsLoading ? (
            <p className="loading">센서 데이터 로딩 중...</p>
          ) : (
            sensors?.map((s) => (
              <SensorCard
                key={s.sensor}
                sensor={s}
                isSelected={s.sensor === selectedSensor}
                onClick={setSelectedSensor}
              />
            ))
          )}
        </section>

        {/* ── 3D Process View (Unity WebGL) ── */}
        <Unity3DView sensors={sensors} pipelineResult={pipelineResult} />

        {/* ── Pipeline Simulation ── */}
        <section className="pipeline-section">
          <h2 className="section-title">🏭 하수 처리 공정 시뮬레이션</h2>
          <p className="pipeline-subtitle">
            실제 수처리 공정 데이터 기반 — 각 공정의 체류시간(HRT)을 조절하면 수질 처리 결과가 연쇄적으로 변합니다.
          </p>

          <div className="pipeline-flow-area">
            {pipelineLoading ? (
              <p className="loading">파이프라인 계산 중...</p>
            ) : (
              <ProcessFlowDiagram
                pipelineResult={pipelineResult}
                selectedStage={selectedStage}
                onSelectStage={setSelectedStage}
              />
            )}
          </div>

          <div className="pipeline-body">
            <div className="pipeline-controls-area">
              <HRTControls hrtRatios={hrtRatios} onChange={handleHRTChange} />
            </div>
            <div className="pipeline-comparison-area">
              <WaterQualityComparison
                pipelineResult={pipelineResult}
                selectedStage={selectedStage}
              />
            </div>
          </div>
        </section>

        {/* ── Chart + Alerts ── */}
        <section className="dashboard-body">
          <div className="chart-area">
            <HistoryChart sensor={selectedSensor} pollInterval={3000} />
          </div>
          <div className="alert-area">
            <AlertPanel alerts={alerts} />
          </div>
        </section>
      </main>

      <footer className="app-footer">
        <span>AquaView v0.1.0</span>
        <span className="footer-divider">|</span>
        <span>수처리 공정 모니터링 포트폴리오 프로젝트</span>
        <span className="footer-divider">|</span>
        <span>FastAPI · React · Unity WebGL</span>
      </footer>
    </div>
  );
}

export default App;
