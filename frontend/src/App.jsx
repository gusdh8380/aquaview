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

function App() {
  const [selectedSensor, setSelectedSensor] = useState("ph");
  const [selectedStage, setSelectedStage] = useState("aeration");
  const [hrtRatios, setHrtRatios] = useState(DEFAULT_HRT_RATIOS);
  const [pipelineResult, setPipelineResult] = useState(null);
  const [pipelineLoading, setPipelineLoading] = useState(true);

  const { data: sensors, loading: sensorsLoading } = usePolling(fetchSensors, 3000);
  const { data: alerts } = usePolling(fetchAlerts, 3000);

  // Initial pipeline fetch (once on mount)
  useEffect(() => {
    fetchPipeline()
      .then((result) => {
        setPipelineResult(result);
        setPipelineLoading(false);
      })
      .catch((err) => {
        console.error("Pipeline fetch failed:", err);
        setPipelineLoading(false);
      });
  }, []);

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

  return (
    <div className="app">
      <header className="app-header">
        <h1>💧 AquaView</h1>
        <p>수처리 공정 모니터링 시스템</p>
      </header>

      <main className="app-main">
        {/* ── Sensor Cards (real-time polling) ── */}
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
        <Unity3DView sensors={sensors} />

        {/* ══════════════════════════════════════════════
            PIPELINE SIMULATION SECTION
        ══════════════════════════════════════════════ */}
        <section className="pipeline-section">
          <h2 className="section-title">🏭 하수 처리 공정 시뮬레이션</h2>
          <p className="pipeline-subtitle">
            실제 수처리 공정 데이터 기반 — 각 공정의 체류시간(HRT)을 조절하면 수질 처리 결과가 연쇄적으로 변합니다.
          </p>

          {/* Process Flow Diagram (SVG) */}
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

          {/* HRT Controls + Water Quality Comparison */}
          <div className="pipeline-body">
            <div className="pipeline-controls-area">
              <HRTControls
                hrtRatios={hrtRatios}
                onChange={handleHRTChange}
              />
            </div>
            <div className="pipeline-comparison-area">
              <WaterQualityComparison
                pipelineResult={pipelineResult}
                selectedStage={selectedStage}
              />
            </div>
          </div>
        </section>

        {/* ── Chart + Alerts layout ── */}
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
        AquaView MVP &mdash; Portfolio Project
      </footer>
    </div>
  );
}

export default App;
