const STATUS_COLORS = {
  warning: "#f59e0b",
  danger: "#ef4444",
};

const STATUS_ICONS = {
  warning: "⚠️",
  danger: "🚨",
};

export default function AlertPanel({ alerts }) {
  if (!alerts || alerts.length === 0) {
    return (
      <div className="alert-panel alert-panel-empty">
        <h3>🔔 경보 현황</h3>
        <p className="no-alerts">✅ 모든 센서 정상 가동 중</p>
      </div>
    );
  }

  return (
    <div className="alert-panel">
      <h3>🔔 경보 현황 ({alerts.length}건)</h3>
      <ul className="alert-list">
        {alerts.map((alert, i) => (
          <li
            key={`${alert.sensor}-${i}`}
            className="alert-item"
            style={{ borderLeft: `4px solid ${STATUS_COLORS[alert.status]}` }}
          >
            <span className="alert-icon">{STATUS_ICONS[alert.status]}</span>
            <span className="alert-message">{alert.message}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
