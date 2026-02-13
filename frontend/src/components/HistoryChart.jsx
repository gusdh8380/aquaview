import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
} from "recharts";
import { useEffect, useState } from "react";
import { fetchHistory } from "../api/client";

const SENSOR_NAMES = {
  ph: "pH",
  turbidity: "탁도 (NTU)",
  flow: "유량 (m³/h)",
  temp: "수온 (°C)",
};

const THRESHOLDS = {
  ph: { normalLo: 6.5, normalHi: 8.5 },
  turbidity: { normalLo: 0, normalHi: 5 },
  flow: { normalLo: 50, normalHi: 150 },
  temp: { normalLo: 15, normalHi: 25 },
};

export default function HistoryChart({ sensor, pollInterval = 3000 }) {
  const [data, setData] = useState([]);

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const result = await fetchHistory(sensor, 20);
        if (active) {
          setData(
            result.data.map((d, i) => ({
              idx: i + 1,
              value: d.value,
              time: new Date(d.timestamp).toLocaleTimeString("ko-KR", {
                hour: "2-digit",
                minute: "2-digit",
                second: "2-digit",
              }),
            }))
          );
        }
      } catch {
        /* ignore */
      }
    }

    load();
    const id = setInterval(load, pollInterval);
    return () => {
      active = false;
      clearInterval(id);
    };
  }, [sensor, pollInterval]);

  const threshold = THRESHOLDS[sensor];

  return (
    <div className="history-chart">
      <h3>📈 {SENSOR_NAMES[sensor]} 이력</h3>
      {data.length === 0 ? (
        <p className="chart-empty">데이터 수집 중...</p>
      ) : (
        <ResponsiveContainer width="100%" height={280}>
          <LineChart data={data} margin={{ top: 10, right: 20, bottom: 10, left: 10 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#333" />
            <XAxis dataKey="time" tick={{ fill: "#aaa", fontSize: 11 }} />
            <YAxis tick={{ fill: "#aaa", fontSize: 12 }} domain={["auto", "auto"]} />
            <Tooltip
              contentStyle={{ backgroundColor: "#1e293b", border: "1px solid #475569" }}
              labelStyle={{ color: "#94a3b8" }}
            />
            <ReferenceLine
              y={threshold.normalLo}
              stroke="#f59e0b"
              strokeDasharray="5 5"
              label={{ value: "하한", fill: "#f59e0b", fontSize: 11 }}
            />
            <ReferenceLine
              y={threshold.normalHi}
              stroke="#f59e0b"
              strokeDasharray="5 5"
              label={{ value: "상한", fill: "#f59e0b", fontSize: 11 }}
            />
            <Line
              type="monotone"
              dataKey="value"
              stroke="#38bdf8"
              strokeWidth={2}
              dot={{ r: 3, fill: "#38bdf8" }}
              activeDot={{ r: 5 }}
            />
          </LineChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}
