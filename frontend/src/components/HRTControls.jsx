/**
 * HRTControls — Sliders for adjusting HRT ratio per treatment stage.
 * hrt_ratio: 0.25–2.5 (1.0 = design HRT 100%)
 */
import { useCallback, useRef } from "react";

const STAGES = [
  {
    key: "primary_settling",
    name: "1차 침전",
    icon: "🪣",
    design_hrt: 2.0,
    unit: "h",
    min: 0.25,
    max: 2.5,
    step: 0.05,
    desc: "중력 침전으로 부유물질 제거",
  },
  {
    key: "aeration",
    name: "폭기조",
    icon: "💨",
    design_hrt: 6.0,
    unit: "h",
    min: 0.25,
    max: 2.5,
    step: 0.05,
    desc: "미생물로 유기물 분해 (BOD/COD 주요 제거)",
  },
  {
    key: "secondary_settling",
    name: "2차 침전",
    icon: "🫧",
    design_hrt: 1.5,
    unit: "h",
    min: 0.25,
    max: 2.5,
    step: 0.05,
    desc: "활성슬러지 분리 및 TSS 추가 제거",
  },
  {
    key: "nitrification",
    name: "질산화",
    icon: "🔬",
    design_hrt: 10.0,
    unit: "h",
    min: 0.25,
    max: 2.5,
    step: 0.05,
    desc: "암모니아→질산염 변환 (NH₃ 제거)",
    critical: true, // very sensitive to HRT
  },
  {
    key: "disinfection",
    name: "소독",
    icon: "☣️",
    design_hrt: 0.5,
    unit: "h",
    min: 0.25,
    max: 2.5,
    step: 0.05,
    desc: "염소 접촉시간으로 병원균 제거",
  },
];

function getRatioColor(ratio) {
  if (ratio < 0.5) return "#ef4444";   // danger: too short
  if (ratio < 0.7) return "#f59e0b";   // warning
  if (ratio > 2.0) return "#a78bfa";   // over-design (purple)
  return "#22c55e";                    // normal
}

function getRatioLabel(ratio) {
  if (ratio < 0.5) return "⚠️ 처리 불량 위험";
  if (ratio < 0.7) return "주의";
  if (ratio > 2.0) return "과설계 (에너지 낭비)";
  return "정상";
}

export default function HRTControls({ hrtRatios, onChange }) {
  // debounce timer ref
  const timerRef = useRef(null);

  const handleChange = useCallback((stageKey, newRatio) => {
    const updated = { ...hrtRatios, [stageKey]: newRatio };
    onChange(updated, false); // immediate local state update

    // debounce API call
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      onChange(updated, true); // trigger API call
    }, 300);
  }, [hrtRatios, onChange]);

  return (
    <div className="hrt-controls">
      <h3 className="hrt-title">⏱ 공정별 체류시간 (HRT) 조절</h3>
      <p className="hrt-subtitle">슬라이더를 조절하면 각 공정의 체류시간이 변경되어 수질 처리 결과가 실시간으로 바뀝니다.</p>

      <div className="hrt-sliders">
        {STAGES.map((stage) => {
          const ratio = hrtRatios[stage.key] ?? 1.0;
          const actualHrt = (stage.design_hrt * ratio).toFixed(1);
          const color = getRatioColor(ratio);
          const label = getRatioLabel(ratio);
          const pct = Math.round(ratio * 100);

          return (
            <div key={stage.key} className="hrt-slider-row">
              <div className="hrt-slider-header">
                <span className="hrt-stage-icon">{stage.icon}</span>
                <span className="hrt-stage-name">{stage.name}</span>
                {stage.critical && (
                  <span className="hrt-critical-badge">민감</span>
                )}
                <span className="hrt-status-label" style={{ color }}>
                  {label}
                </span>
                <span className="hrt-value" style={{ color }}>
                  {actualHrt}h ({pct}%)
                </span>
              </div>

              <div className="hrt-slider-desc">{stage.desc}</div>

              <div className="hrt-slider-track">
                <span className="hrt-range-label">
                  {(stage.design_hrt * stage.min).toFixed(1)}h
                </span>
                <input
                  type="range"
                  min={stage.min}
                  max={stage.max}
                  step={stage.step}
                  value={ratio}
                  onChange={(e) => handleChange(stage.key, parseFloat(e.target.value))}
                  style={{ "--thumb-color": color }}
                  className="hrt-range-input"
                />
                <span className="hrt-range-label">
                  {(stage.design_hrt * stage.max).toFixed(1)}h
                </span>
              </div>

              {/* Progress bar */}
              <div className="hrt-progress-bar">
                <div
                  className="hrt-progress-fill"
                  style={{
                    width: `${((ratio - stage.min) / (stage.max - stage.min)) * 100}%`,
                    backgroundColor: color,
                  }}
                />
                {/* Design mark at ratio=1.0 */}
                <div
                  className="hrt-design-mark"
                  style={{
                    left: `${((1.0 - stage.min) / (stage.max - stage.min)) * 100}%`,
                  }}
                  title="설계값"
                />
              </div>
            </div>
          );
        })}
      </div>

      <button
        className="hrt-reset-btn"
        onClick={() => {
          const defaults = Object.fromEntries(STAGES.map((s) => [s.key, 1.0]));
          onChange(defaults, true);
        }}
      >
        🔄 설계값으로 초기화
      </button>
    </div>
  );
}
