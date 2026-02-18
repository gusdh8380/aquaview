/**
 * WaterQualityComparison — Raw water vs Treated water side-by-side comparison.
 * Also shows selected stage detail (influent/effluent per stage).
 */

const STATUS_COLORS = {
  normal: "#22c55e",
  warning: "#f59e0b",
  danger: "#ef4444",
};

// Effluent standards (Korean env. regs + EPA secondary)
const STANDARDS = {
  bod: { normal: 10, warning: 20, unit: "mg/L", label: "BOD" },
  tss: { normal: 10, warning: 30, unit: "mg/L", label: "TSS" },
  cod: { normal: 40, warning: 80, unit: "mg/L", label: "COD" },
  ammonia: { normal: 1.0, warning: 5.0, unit: "mg/L", label: "NH₃-N" },
  turbidity: { normal: 2.0, warning: 5.0, unit: "NTU", label: "탁도" },
  ph: { normal: null, warning: null, unit: "", label: "pH" },
  do_level: { normal: null, warning: null, unit: "mg/L", label: "DO" },
  coliform: { normal: 10, warning: 100, unit: "CFU/100mL", label: "대장균" },
};

function getValueStatus(key, val) {
  const std = STANDARDS[key];
  if (!std || std.normal === null) return "normal";
  if (val > std.warning) return "danger";
  if (val > std.normal) return "warning";
  return "normal";
}

function formatValue(key, val) {
  if (key === "coliform") {
    if (val >= 1_000_000) return `${(val / 1_000_000).toFixed(1)}×10⁶`;
    if (val >= 1000) return `${(val / 1000).toFixed(1)}×10³`;
    if (val < 10) return `${val.toFixed(1)}`;
    return val.toFixed(0);
  }
  if (key === "ph") return val.toFixed(2);
  if (key === "do_level") return val.toFixed(2);
  if (key === "ammonia") return val < 1 ? val.toFixed(3) : val.toFixed(2);
  if (val < 1) return val.toFixed(2);
  return val.toFixed(1);
}

function MetricRow({ label, unit, rawVal, treatedVal, metricKey }) {
  const rawStatus = getValueStatus(metricKey, rawVal);
  const treatedStatus = getValueStatus(metricKey, treatedVal);
  const treatedColor = STATUS_COLORS[treatedStatus];

  const removal = rawVal > 0 ? ((rawVal - treatedVal) / rawVal * 100) : 0;
  const improved = treatedVal <= rawVal;

  return (
    <div className="wq-metric-row">
      <span className="wq-metric-label">{label}</span>
      <span className="wq-raw-val">{formatValue(metricKey, rawVal)} <span className="wq-unit">{unit}</span></span>
      <span className="wq-arrow">→</span>
      <span className="wq-treated-val" style={{ color: treatedColor }}>
        {formatValue(metricKey, treatedVal)} <span className="wq-unit">{unit}</span>
      </span>
      {improved && removal > 0 ? (
        <span className="wq-removal" style={{ color: treatedColor }}>
          ↓{removal.toFixed(0)}%
        </span>
      ) : (
        <span className="wq-removal wq-removal-none">—</span>
      )}
    </div>
  );
}

function StageDetail({ stage }) {
  if (!stage) return null;

  const statusColor = STATUS_COLORS[stage.status];

  return (
    <div className="stage-detail" style={{ borderColor: statusColor }}>
      <h4 className="stage-detail-title" style={{ color: statusColor }}>
        📋 {stage.stage_name_ko} 상세
        <span className="stage-hrt-badge">HRT {stage.hrt_hours.toFixed(1)}h ({Math.round(stage.hrt_ratio * 100)}%)</span>
      </h4>

      <div className="stage-detail-grid">
        <div className="stage-col">
          <div className="stage-col-title">유입수</div>
          {Object.entries(STANDARDS).map(([key, std]) => (
            <div key={key} className="stage-metric-line">
              <span className="stage-metric-label">{std.label}</span>
              <span className="stage-metric-val">{formatValue(key, stage.influent[key])} {std.unit}</span>
            </div>
          ))}
        </div>
        <div className="stage-col-arrow">⟹</div>
        <div className="stage-col">
          <div className="stage-col-title" style={{ color: statusColor }}>유출수</div>
          {Object.entries(STANDARDS).map(([key, std]) => {
            const val = stage.effluent[key];
            const s = getValueStatus(key, val);
            return (
              <div key={key} className="stage-metric-line">
                <span className="stage-metric-label">{std.label}</span>
                <span className="stage-metric-val" style={{ color: STATUS_COLORS[s] }}>
                  {formatValue(key, val)} {std.unit}
                </span>
              </div>
            );
          })}
        </div>
        <div className="stage-col">
          <div className="stage-col-title">제거율</div>
          {Object.entries(stage.removal_efficiencies).map(([key, pct]) => (
            <div key={key} className="stage-metric-line">
              <span className="stage-metric-label">{STANDARDS[key]?.label ?? key}</span>
              <span className="stage-metric-val" style={{ color: pct > 0 ? "#22c55e" : "#94a3b8" }}>
                {pct > 0 ? `↓${pct}%` : "—"}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default function WaterQualityComparison({ pipelineResult, selectedStage }) {
  if (!pipelineResult) return null;

  const { raw_water, treated_water, overall_removal, overall_status, stages } = pipelineResult;
  const overallColor = STATUS_COLORS[overall_status];

  const selectedStageData = stages?.find((s) => s.stage === selectedStage);

  return (
    <div className="water-quality-comparison">
      {/* ── Overall comparison card ── */}
      <div className="wq-card">
        <div className="wq-header">
          <h3 className="wq-title">💧 수질 처리 결과 비교</h3>
          <span className="wq-overall-status" style={{ backgroundColor: `${overallColor}22`, color: overallColor, border: `1px solid ${overallColor}` }}>
            {overall_status === "normal" ? "✅ 방류 기준 적합" :
             overall_status === "warning" ? "⚠️ 일부 기준 초과" : "🚨 기준 초과 — 재처리 필요"}
          </span>
        </div>

        <div className="wq-columns-header">
          <span className="wq-metric-label">항목</span>
          <span className="wq-raw-val">원수</span>
          <span className="wq-arrow"></span>
          <span className="wq-treated-val">처리수</span>
          <span className="wq-removal">제거율</span>
        </div>

        <div className="wq-metrics">
          {Object.entries(STANDARDS).map(([key, std]) => (
            <MetricRow
              key={key}
              label={std.label}
              unit={std.unit}
              rawVal={raw_water[key]}
              treatedVal={treated_water[key]}
              metricKey={key}
            />
          ))}
        </div>

        {/* Overall removal summary */}
        <div className="wq-summary">
          {Object.entries(overall_removal).map(([key, pct]) => (
            <div key={key} className="wq-summary-chip">
              <span className="wq-summary-label">{STANDARDS[key]?.label ?? key}</span>
              <span className="wq-summary-pct" style={{ color: pct >= 90 ? "#22c55e" : pct >= 70 ? "#f59e0b" : "#ef4444" }}>
                {pct.toFixed(0)}%↓
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* ── Selected stage detail ── */}
      {selectedStageData && (
        <StageDetail stage={selectedStageData} />
      )}
    </div>
  );
}
