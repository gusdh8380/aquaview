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

  return (
    <div
      className="sensor-card"
      style={{
        borderColor: isSelected ? color : `${color}40`,
        cursor: "pointer",
        boxShadow: isSelected ? `0 0 0 1px ${color}40` : "none",
      }}
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
