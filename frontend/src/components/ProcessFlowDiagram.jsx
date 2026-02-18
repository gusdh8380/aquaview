/**
 * ProcessFlowDiagram — SVG-based wastewater treatment pipeline visualization
 * Shows: Raw Water → [5 stages] → Treated Water
 * Each stage box is color-coded by status and clickable.
 */

const STATUS_COLORS = {
  normal: "#22c55e",
  warning: "#f59e0b",
  danger: "#ef4444",
};

const STAGE_ICONS = {
  primary_settling: "🪣",
  aeration: "💨",
  secondary_settling: "🫧",
  nitrification: "🔬",
  disinfection: "☣️",
};

const STAGE_SHORT = {
  primary_settling: "1차 침전",
  aeration: "폭기조",
  secondary_settling: "2차 침전",
  nitrification: "질산화",
  disinfection: "소독",
};

// Key metric to show under each stage box
const STAGE_KEY_METRIC = {
  primary_settling: (s) => `BOD ${s.effluent.bod.toFixed(1)} mg/L`,
  aeration: (s) => `BOD ${s.effluent.bod.toFixed(1)} mg/L`,
  secondary_settling: (s) => `TSS ${s.effluent.tss.toFixed(1)} mg/L`,
  nitrification: (s) => `NH₃ ${s.effluent.ammonia.toFixed(2)} mg/L`,
  disinfection: (s) => `탁도 ${s.effluent.turbidity.toFixed(2)} NTU`,
};

export default function ProcessFlowDiagram({ pipelineResult, selectedStage, onSelectStage }) {
  if (!pipelineResult) {
    return (
      <div className="process-flow-loading">
        <p>파이프라인 데이터 로딩 중...</p>
      </div>
    );
  }

  const { stages, raw_water, treated_water, overall_status } = pipelineResult;

  // SVG layout constants
  const SVG_W = 900;
  const SVG_H = 180;
  const BOX_W = 110;
  const BOX_H = 70;
  const BOX_Y = 55;
  const ARROW_Y = BOX_Y + BOX_H / 2;

  // Positions: raw | stage0 | stage1 | stage2 | stage3 | stage4 | treated
  const totalSlots = 7;
  const slotW = SVG_W / totalSlots;
  const centerX = (i) => slotW * i + slotW / 2;

  const rawX = centerX(0);
  const stageXs = stages.map((_, i) => centerX(i + 1));
  const treatedX = centerX(6);

  const endpointBoxW = 80;
  const endpointBoxH = 56;
  const endpointY = BOX_Y + (BOX_H - endpointBoxH) / 2;

  const treatedColor = STATUS_COLORS[overall_status] ?? "#38bdf8";

  return (
    <div className="process-flow-diagram">
      <svg
        viewBox={`0 0 ${SVG_W} ${SVG_H}`}
        width="100%"
        preserveAspectRatio="xMidYMid meet"
        style={{ overflow: "visible" }}
      >
        <defs>
          {/* Animated flow gradient */}
          <linearGradient id="flowGrad" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#38bdf8" stopOpacity="0.3" />
            <stop offset="50%" stopColor="#38bdf8" stopOpacity="0.9" />
            <stop offset="100%" stopColor="#38bdf8" stopOpacity="0.3" />
            <animateTransform
              attributeName="gradientTransform"
              type="translate"
              values="-1 0;1 0;-1 0"
              dur="2s"
              repeatCount="indefinite"
            />
          </linearGradient>
          <marker id="arrowhead" markerWidth="7" markerHeight="7" refX="3" refY="3.5" orient="auto">
            <polygon points="0 0, 7 3.5, 0 7" fill="#38bdf8" opacity="0.7" />
          </marker>
        </defs>

        {/* ── Connecting arrows (pipes) ── */}
        {/* raw → stage0 */}
        <line
          x1={rawX + endpointBoxW / 2} y1={ARROW_Y}
          x2={stageXs[0] - BOX_W / 2 - 4} y2={ARROW_Y}
          stroke="url(#flowGrad)" strokeWidth="4" markerEnd="url(#arrowhead)"
        />
        {/* stage → stage */}
        {stages.slice(0, -1).map((_, i) => (
          <line
            key={`arrow-${i}`}
            x1={stageXs[i] + BOX_W / 2 + 2} y1={ARROW_Y}
            x2={stageXs[i + 1] - BOX_W / 2 - 4} y2={ARROW_Y}
            stroke="url(#flowGrad)" strokeWidth="4" markerEnd="url(#arrowhead)"
          />
        ))}
        {/* last stage → treated */}
        <line
          x1={stageXs[4] + BOX_W / 2 + 2} y1={ARROW_Y}
          x2={treatedX - endpointBoxW / 2 - 4} y2={ARROW_Y}
          stroke="url(#flowGrad)" strokeWidth="4" markerEnd="url(#arrowhead)"
        />

        {/* ── Raw water box ── */}
        <rect
          x={rawX - endpointBoxW / 2} y={endpointY}
          width={endpointBoxW} height={endpointBoxH}
          rx="8" fill="#1e293b" stroke="#64748b" strokeWidth="2"
        />
        <text x={rawX} y={endpointY + 18} textAnchor="middle" fill="#94a3b8" fontSize="11" fontWeight="600">
          원수
        </text>
        <text x={rawX} y={endpointY + 32} textAnchor="middle" fill="#64748b" fontSize="9.5">
          BOD 200
        </text>
        <text x={rawX} y={endpointY + 44} textAnchor="middle" fill="#64748b" fontSize="9.5">
          TSS 220 mg/L
        </text>

        {/* ── Stage boxes ── */}
        {stages.map((stage, i) => {
          const x = stageXs[i];
          const color = STATUS_COLORS[stage.status];
          const isSelected = selectedStage === stage.stage;
          const metricFn = STAGE_KEY_METRIC[stage.stage];
          const metricText = metricFn ? metricFn(stage) : "";

          return (
            <g
              key={stage.stage}
              onClick={() => onSelectStage?.(stage.stage)}
              style={{ cursor: "pointer" }}
            >
              {/* Glow / selection ring */}
              {isSelected && (
                <rect
                  x={x - BOX_W / 2 - 4} y={BOX_Y - 4}
                  width={BOX_W + 8} height={BOX_H + 8}
                  rx="12" fill="none"
                  stroke={color} strokeWidth="2.5" strokeDasharray="4 2"
                  opacity="0.8"
                />
              )}
              {/* Main box */}
              <rect
                x={x - BOX_W / 2} y={BOX_Y}
                width={BOX_W} height={BOX_H}
                rx="10"
                fill={isSelected ? `${color}22` : "#1e293b"}
                stroke={color}
                strokeWidth={isSelected ? 2.5 : 1.5}
              />
              {/* Icon */}
              <text x={x} y={BOX_Y + 22} textAnchor="middle" fontSize="16">
                {STAGE_ICONS[stage.stage]}
              </text>
              {/* Stage name */}
              <text x={x} y={BOX_Y + 39} textAnchor="middle" fill="#e2e8f0" fontSize="10" fontWeight="600">
                {STAGE_SHORT[stage.stage]}
              </text>
              {/* Key metric */}
              <text x={x} y={BOX_Y + 53} textAnchor="middle" fill={color} fontSize="9">
                {metricText}
              </text>
              {/* HRT label below box */}
              <text x={x} y={BOX_Y + BOX_H + 18} textAnchor="middle" fill="#64748b" fontSize="9">
                {stage.hrt_hours.toFixed(1)}h
              </text>
            </g>
          );
        })}

        {/* ── Treated water box ── */}
        <rect
          x={treatedX - endpointBoxW / 2} y={endpointY}
          width={endpointBoxW} height={endpointBoxH}
          rx="8"
          fill={`${treatedColor}18`}
          stroke={treatedColor}
          strokeWidth="2"
        />
        <text x={treatedX} y={endpointY + 18} textAnchor="middle" fill={treatedColor} fontSize="11" fontWeight="700">
          처리수
        </text>
        <text x={treatedX} y={endpointY + 32} textAnchor="middle" fill={treatedColor} fontSize="9.5">
          BOD {treated_water.bod.toFixed(1)}
        </text>
        <text x={treatedX} y={endpointY + 44} textAnchor="middle" fill={treatedColor} fontSize="9.5">
          TSS {treated_water.tss.toFixed(1)} mg/L
        </text>
      </svg>
    </div>
  );
}
