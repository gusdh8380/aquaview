const STATUS_COLORS = {
  normal: "#22c55e",
  warning: "#f59e0b",
  danger: "#ef4444",
};

const STATUS_LABELS = {
  normal: "정상",
  warning: "경고",
  danger: "위험",
};

const SENSOR_NAMES = {
  ph: "pH",
  turbidity: "탁도",
  flow: "유량",
  temp: "수온",
};

const SENSOR_ICONS = {
  ph: "🧪",
  turbidity: "💧",
  flow: "🌊",
  temp: "🌡️",
};

export default function SensorCard({ sensor, onClick, isSelected }) {
  const color = STATUS_COLORS[sensor.status];
  const borderStyle = isSelected ? `3px solid ${color}` : `2px solid ${color}40`;

  return (
    <div
      className="sensor-card"
      style={{ border: borderStyle, cursor: "pointer" }}
      onClick={() => onClick?.(sensor.sensor)}
    >
      <div className="sensor-card-header">
        <span className="sensor-icon">{SENSOR_ICONS[sensor.sensor]}</span>
        <span className="sensor-name">{SENSOR_NAMES[sensor.sensor]}</span>
        <span
          className="sensor-status-badge"
          style={{ backgroundColor: color }}
        >
          {STATUS_LABELS[sensor.status]}
        </span>
      </div>
      <div className="sensor-value" style={{ color }}>
        {sensor.value}
        <span className="sensor-unit">{sensor.unit}</span>
      </div>
    </div>
  );
}
