import { useState } from "react";
import usePolling from "./hooks/usePolling";
import { fetchSensors, fetchAlerts } from "./api/client";
import SensorCard from "./components/SensorCard";
import HistoryChart from "./components/HistoryChart";
import AlertPanel from "./components/AlertPanel";
import "./App.css";

function App() {
  const [selectedSensor, setSelectedSensor] = useState("ph");

  const { data: sensors, loading: sensorsLoading } = usePolling(fetchSensors, 3000);
  const { data: alerts } = usePolling(fetchAlerts, 3000);

  return (
    <div className="app">
      <header className="app-header">
        <h1>💧 AquaView</h1>
        <p>수처리 공정 모니터링 시스템</p>
      </header>

      <main className="app-main">
        {/* Sensor Cards */}
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

        {/* Chart + Alerts layout */}
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
