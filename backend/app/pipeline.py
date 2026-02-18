"""
AquaView — Wastewater Treatment Pipeline Simulation Engine

Based on real municipal wastewater treatment data:
- EPA Secondary Treatment Standards
- Typical HRT ranges from engineering references
- Quantitative BOD/TSS/COD/NH3 removal curves

Pipeline stages (in order):
  1. Primary Settling    (HRT 1–4h,   design 2h)
  2. Aeration            (HRT 3–12h,  design 6h)
  3. Secondary Settling  (HRT 0.75–3h, design 1.5h)
  4. Nitrification       (HRT 5–20h,  design 10h)
  5. Disinfection        (CT 0.25–1h, design 0.5h)
"""

from __future__ import annotations

import math

from .models import (
    ProcessStage,
    SensorStatus,
    StageParams,
    StageResult,
    WaterQuality,
    PipelineResult,
)

# ── Design HRT (hours) per stage ────────────────────────────────────
DESIGN_HRT: dict[ProcessStage, float] = {
    ProcessStage.PRIMARY_SETTLING: 2.0,
    ProcessStage.AERATION: 6.0,
    ProcessStage.SECONDARY_SETTLING: 1.5,
    ProcessStage.NITRIFICATION: 10.0,
    ProcessStage.DISINFECTION: 0.5,
}

STAGE_ORDER = [
    ProcessStage.PRIMARY_SETTLING,
    ProcessStage.AERATION,
    ProcessStage.SECONDARY_SETTLING,
    ProcessStage.NITRIFICATION,
    ProcessStage.DISINFECTION,
]

STAGE_NAMES_KO: dict[ProcessStage, str] = {
    ProcessStage.PRIMARY_SETTLING: "1차 침전",
    ProcessStage.AERATION: "폭기조 (생물학적 처리)",
    ProcessStage.SECONDARY_SETTLING: "2차 침전",
    ProcessStage.NITRIFICATION: "질산화 (고도처리)",
    ProcessStage.DISINFECTION: "소독",
}

# ── Raw wastewater (typical municipal influent) ──────────────────────
# Source: EPA, WEF engineering references
RAW_WATER = WaterQuality(
    bod=200.0,          # mg/L  (typical: 150–300)
    tss=220.0,          # mg/L  (typical: 150–300)
    cod=400.0,          # mg/L  (typical: 300–600)
    ammonia=35.0,       # mg/L  NH3-N (typical: 20–50)
    turbidity=50.0,     # NTU   (typical: 40–80)
    ph=7.2,             # pH    (typical: 6.5–8.0)
    do_level=0.5,       # mg/L  DO (very low in raw sewage)
    coliform=1_000_000.0,  # CFU/100mL (10^6, typical raw sewage)
)

# ── Effluent quality standards (Korean & EPA secondary) ─────────────
EFFLUENT_STANDARDS = {
    "bod": {"normal": 10.0, "warning": 20.0},       # mg/L
    "tss": {"normal": 10.0, "warning": 30.0},       # mg/L
    "cod": {"normal": 40.0, "warning": 80.0},       # mg/L
    "ammonia": {"normal": 1.0, "warning": 5.0},     # mg/L
    "turbidity": {"normal": 2.0, "warning": 5.0},   # NTU
    "coliform": {"normal": 10.0, "warning": 100.0}, # CFU/100mL
}


def _clamp(value: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, value))


def _sigmoid_removal(hrt_ratio: float, r_min: float, r_max: float,
                     steepness: float = 3.0, midpoint: float = 1.0) -> float:
    """
    Sigmoid-shaped removal efficiency curve.
    hrt_ratio=1.0 → ~70% between r_min and r_max
    hrt_ratio→∞   → r_max
    hrt_ratio→0   → r_min
    """
    x = steepness * (hrt_ratio - midpoint)
    sigmoid = 1 / (1 + math.exp(-x))
    return r_min + (r_max - r_min) * sigmoid


def _assess_stage_status(effluent: WaterQuality) -> SensorStatus:
    """Assess worst-case stage status based on effluent vs interim targets."""
    # Use relative assessment: any metric very high → danger
    worst = SensorStatus.NORMAL
    checks = [
        (effluent.bod, 50.0, 100.0),
        (effluent.tss, 40.0, 80.0),
        (effluent.cod, 80.0, 180.0),
        (effluent.ammonia, 10.0, 25.0),
        (effluent.turbidity, 10.0, 25.0),
    ]
    for val, warn_thresh, danger_thresh in checks:
        if val >= danger_thresh:
            return SensorStatus.DANGER
        if val >= warn_thresh:
            worst = SensorStatus.WARNING
    return worst


def _removal_efficiencies(influent: WaterQuality, effluent: WaterQuality) -> dict[str, float]:
    """Calculate % removal for each key metric."""
    def pct(before: float, after: float) -> float:
        if before <= 0:
            return 0.0
        return round((before - after) / before * 100, 1)

    return {
        "bod": pct(influent.bod, effluent.bod),
        "tss": pct(influent.tss, effluent.tss),
        "cod": pct(influent.cod, effluent.cod),
        "ammonia": pct(influent.ammonia, effluent.ammonia),
        "turbidity": pct(influent.turbidity, effluent.turbidity),
        "coliform": pct(influent.coliform, effluent.coliform),
    }


# ── Stage calculation functions ──────────────────────────────────────

def _calc_primary_settling(influent: WaterQuality, hrt_ratio: float) -> WaterQuality:
    """
    Primary Settling: gravity sedimentation of settleable solids.
    Design HRT 2h → TSS 65%, BOD 45%, COD 35% removal
    Range: HRT 1–4h (ratio 0.5–2.0)

    References:
    - TSS removal: 60–70% at 2h, max ~75%
    - BOD removal: 40–50% at 2h (particulate fraction), max ~55%
    """
    tss_removal = _sigmoid_removal(hrt_ratio, r_min=0.35, r_max=0.78, steepness=2.5)
    bod_removal = _sigmoid_removal(hrt_ratio, r_min=0.25, r_max=0.58, steepness=2.5)
    cod_removal = _sigmoid_removal(hrt_ratio, r_min=0.20, r_max=0.45, steepness=2.5)
    # Turbidity roughly tracks TSS
    turb_removal = _sigmoid_removal(hrt_ratio, r_min=0.30, r_max=0.70, steepness=2.5)

    return WaterQuality(
        bod=round(influent.bod * (1 - bod_removal), 2),
        tss=round(influent.tss * (1 - tss_removal), 2),
        cod=round(influent.cod * (1 - cod_removal), 2),
        ammonia=round(influent.ammonia * 0.98, 2),   # minimal NH3 change in settling
        turbidity=round(influent.turbidity * (1 - turb_removal), 2),
        ph=round(_clamp(influent.ph + 0.05, 6.0, 9.0), 2),
        do_level=round(_clamp(influent.do_level + 0.2, 0.0, 4.0), 2),
        coliform=round(influent.coliform * 0.85, 0),  # ~15% reduction
    )


def _calc_aeration(influent: WaterQuality, hrt_ratio: float) -> WaterQuality:
    """
    Aeration (Activated Sludge): biological oxidation of organics.
    Design HRT 6h → BOD ~88%, COD ~78% removal
    Range: HRT 3–12h (ratio 0.5–2.0)

    References:
    - HRT 3h  → BOD removal ~75%
    - HRT 6h  → BOD removal ~88%
    - HRT 12h → BOD removal ~95%
    - COD: 6→78%, 12→94%
    - DO rises to 2–4 mg/L with aeration
    """
    bod_removal = _sigmoid_removal(hrt_ratio, r_min=0.55, r_max=0.97, steepness=3.0)
    cod_removal = _sigmoid_removal(hrt_ratio, r_min=0.45, r_max=0.92, steepness=3.0)
    tss_removal = _sigmoid_removal(hrt_ratio, r_min=0.20, r_max=0.55, steepness=2.5)
    # Partial nitrification in aeration at longer HRT
    nh3_removal = _sigmoid_removal(hrt_ratio, r_min=0.05, r_max=0.40, steepness=2.0)
    # DO increases with aeration
    do_out = _clamp(2.0 + hrt_ratio * 0.8, 1.0, 5.0)

    return WaterQuality(
        bod=round(influent.bod * (1 - bod_removal), 2),
        tss=round(influent.tss * (1 - tss_removal), 2),
        cod=round(influent.cod * (1 - cod_removal), 2),
        ammonia=round(influent.ammonia * (1 - nh3_removal), 2),
        turbidity=round(influent.turbidity * 0.70, 2),  # biological floc traps turbidity
        ph=round(_clamp(influent.ph - 0.1 * hrt_ratio, 6.5, 8.5), 2),
        do_level=round(do_out, 2),
        coliform=round(influent.coliform * 0.80, 0),  # ~20% reduction
    )


def _calc_secondary_settling(influent: WaterQuality, hrt_ratio: float) -> WaterQuality:
    """
    Secondary Settling: separates biological sludge from treated water.
    Design HRT 1.5h → TSS ~80% cumulative
    Range: HRT 0.75–3h (ratio 0.5–2.0)
    """
    tss_removal = _sigmoid_removal(hrt_ratio, r_min=0.50, r_max=0.88, steepness=3.0)
    bod_removal = _sigmoid_removal(hrt_ratio, r_min=0.20, r_max=0.50, steepness=2.5)
    turb_removal = _sigmoid_removal(hrt_ratio, r_min=0.40, r_max=0.80, steepness=3.0)

    return WaterQuality(
        bod=round(influent.bod * (1 - bod_removal), 2),
        tss=round(influent.tss * (1 - tss_removal), 2),
        cod=round(influent.cod * 0.85, 2),
        ammonia=round(influent.ammonia * 0.97, 2),
        turbidity=round(influent.turbidity * (1 - turb_removal), 2),
        ph=round(_clamp(influent.ph, 6.5, 8.5), 2),
        do_level=round(_clamp(influent.do_level - 0.3, 1.0, 5.0), 2),
        coliform=round(influent.coliform * 0.70, 0),
    )


def _calc_nitrification(influent: WaterQuality, hrt_ratio: float) -> WaterQuality:
    """
    Nitrification: biological conversion of NH3 → NO3.
    Design HRT 10h → NH3 removal ~90%
    Range: HRT 5–20h (ratio 0.5–2.0)

    Critical: nitrifiers are slow-growing; very short HRT → washout
    - HRT 3h (ratio 0.3): ~38% removal (washout risk)
    - HRT 6h (ratio 0.6): ~80% removal
    - HRT 10h (ratio 1.0): ~90% removal
    - HRT 15h (ratio 1.5): ~95% removal
    References: Wang et al. 2020 (Water 12, 650)
    """
    # Steep sigmoid: very sensitive to low HRT (nitrifier washout)
    nh3_removal = _sigmoid_removal(hrt_ratio, r_min=0.10, r_max=0.97, steepness=4.5, midpoint=0.8)
    # pH drops slightly during nitrification (acid produced)
    ph_drop = 0.3 * nh3_removal
    # DO consumed by nitrification
    do_out = _clamp(influent.do_level - 1.0 * nh3_removal + 1.5, 1.0, 5.0)

    return WaterQuality(
        bod=round(influent.bod * 0.90, 2),   # small additional BOD reduction
        tss=round(influent.tss * 0.92, 2),
        cod=round(influent.cod * 0.88, 2),
        ammonia=round(influent.ammonia * (1 - nh3_removal), 3),
        turbidity=round(influent.turbidity * 0.85, 2),
        ph=round(_clamp(influent.ph - ph_drop, 6.0, 8.5), 2),
        do_level=round(do_out, 2),
        coliform=round(influent.coliform * 0.60, 0),
    )


def _calc_disinfection(influent: WaterQuality, hrt_ratio: float) -> WaterQuality:
    """
    Disinfection: chlorination CT-based pathogen removal.
    Design HRT 0.5h (30 min contact time) → coliform reduction 99.99%
    Range: 0.25–1h (ratio 0.5–2.0)
    CT = chlorine conc (5 mg/L) × time (min)

    - CT low (ratio 0.5, ~15 min): ~3 log reduction (99.9%)
    - CT design (ratio 1.0, ~30 min): ~4 log reduction (99.99%)
    - CT high (ratio 2.0, ~60 min): ~5 log reduction (99.999%)
    """
    # log reduction of coliform (CT-based)
    log_reduction = _sigmoid_removal(hrt_ratio, r_min=2.0, r_max=5.5, steepness=3.0)
    coliform_out = max(1.0, influent.coliform / (10 ** log_reduction))

    # Turbidity: filtration before disinfection achieves <2 NTU target
    turb_removal = _sigmoid_removal(hrt_ratio, r_min=0.50, r_max=0.97, steepness=3.0)
    # Residual chlorine raises pH slightly
    ph_out = _clamp(influent.ph + 0.1, 6.5, 8.5)

    return WaterQuality(
        bod=round(influent.bod * 0.85, 2),   # some BOD from sand filter
        tss=round(influent.tss * 0.80, 2),   # tertiary filtration
        cod=round(influent.cod * 0.85, 2),
        ammonia=round(influent.ammonia * 0.95, 3),
        turbidity=round(influent.turbidity * (1 - turb_removal), 2),
        ph=round(ph_out, 2),
        do_level=round(_clamp(influent.do_level + 0.5, 3.0, 8.0), 2),
        coliform=round(coliform_out, 1),
    )


_CALC_FUNCTIONS = {
    ProcessStage.PRIMARY_SETTLING: _calc_primary_settling,
    ProcessStage.AERATION: _calc_aeration,
    ProcessStage.SECONDARY_SETTLING: _calc_secondary_settling,
    ProcessStage.NITRIFICATION: _calc_nitrification,
    ProcessStage.DISINFECTION: _calc_disinfection,
}


def _assess_final_status(treated: WaterQuality) -> SensorStatus:
    """Assess overall pipeline status against effluent standards."""
    worst = SensorStatus.NORMAL
    for metric, standards in EFFLUENT_STANDARDS.items():
        val = getattr(treated, metric)
        if val > standards["warning"]:
            return SensorStatus.DANGER
        if val > standards["normal"]:
            worst = SensorStatus.WARNING
    return worst


def _overall_removal(raw: WaterQuality, treated: WaterQuality) -> dict[str, float]:
    def pct(before: float, after: float) -> float:
        if before <= 0:
            return 0.0
        return round((before - after) / before * 100, 1)

    return {
        "bod": pct(raw.bod, treated.bod),
        "tss": pct(raw.tss, treated.tss),
        "cod": pct(raw.cod, treated.cod),
        "ammonia": pct(raw.ammonia, treated.ammonia),
        "turbidity": pct(raw.turbidity, treated.turbidity),
        "coliform": pct(raw.coliform, treated.coliform),
    }


# ── Public API ───────────────────────────────────────────────────────

DEFAULT_PARAMS: list[StageParams] = [
    StageParams(stage=s, hrt_ratio=1.0) for s in STAGE_ORDER
]


def run_pipeline(params: list[StageParams] | None = None) -> PipelineResult:
    """
    Run the full pipeline simulation with given HRT ratios.
    Returns WaterQuality at each stage and final treated water.
    """
    if params is None:
        params = DEFAULT_PARAMS

    # Build a lookup for quick access
    param_map: dict[ProcessStage, float] = {p.stage: p.hrt_ratio for p in params}

    current: WaterQuality = RAW_WATER
    stage_results: list[StageResult] = []

    for stage in STAGE_ORDER:
        hrt_ratio = param_map.get(stage, 1.0)
        hrt_hours = DESIGN_HRT[stage] * hrt_ratio
        influent = current

        calc_fn = _CALC_FUNCTIONS[stage]
        effluent = calc_fn(influent, hrt_ratio)

        efficiencies = _removal_efficiencies(influent, effluent)
        status = _assess_stage_status(effluent)

        stage_results.append(StageResult(
            stage=stage,
            stage_name_ko=STAGE_NAMES_KO[stage],
            hrt_ratio=round(hrt_ratio, 3),
            hrt_hours=round(hrt_hours, 2),
            influent=influent,
            effluent=effluent,
            removal_efficiencies=efficiencies,
            status=status,
        ))

        current = effluent

    treated = current
    return PipelineResult(
        raw_water=RAW_WATER,
        stages=stage_results,
        treated_water=treated,
        overall_removal=_overall_removal(RAW_WATER, treated),
        overall_status=_assess_final_status(treated),
    )
