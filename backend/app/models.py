"""Pydantic models for AquaView API responses."""

from __future__ import annotations

from datetime import datetime
from enum import Enum
from pydantic import BaseModel, Field


class SensorStatus(str, Enum):
    NORMAL = "normal"
    WARNING = "warning"
    DANGER = "danger"


class SensorType(str, Enum):
    PH = "ph"
    TURBIDITY = "turbidity"
    FLOW = "flow"
    TEMP = "temp"


class SensorData(BaseModel):
    """Single sensor reading."""
    sensor: SensorType
    value: float
    unit: str
    status: SensorStatus
    timestamp: datetime


class SensorResponse(BaseModel):
    """Response for GET /api/sensors."""
    sensors: list[SensorData]


class Alert(BaseModel):
    """An alert for a sensor in warning/danger state."""
    sensor: SensorType
    value: float
    unit: str
    status: SensorStatus
    message: str
    timestamp: datetime


class AlertResponse(BaseModel):
    """Response for GET /api/alerts."""
    alerts: list[Alert]


class HistoryEntry(BaseModel):
    """A single history data point."""
    value: float
    status: SensorStatus
    timestamp: datetime


class HistoryResponse(BaseModel):
    """Response for GET /api/history."""
    sensor: SensorType
    unit: str
    data: list[HistoryEntry]


# ── Pipeline Models ──────────────────────────────────────────────────


class ProcessStage(str, Enum):
    PRIMARY_SETTLING = "primary_settling"
    AERATION = "aeration"
    SECONDARY_SETTLING = "secondary_settling"
    NITRIFICATION = "nitrification"
    DISINFECTION = "disinfection"


class WaterQuality(BaseModel):
    """Water quality metrics at a point in the pipeline."""
    bod: float = Field(description="BOD (mg/L)")
    tss: float = Field(description="TSS (mg/L)")
    cod: float = Field(description="COD (mg/L)")
    ammonia: float = Field(description="NH3-N (mg/L)")
    turbidity: float = Field(description="탁도 (NTU)")
    ph: float = Field(description="pH")
    do_level: float = Field(description="DO (mg/L)")
    coliform: float = Field(description="대장균 (CFU/100mL)")


class StageParams(BaseModel):
    """HRT parameter for a single process stage."""
    stage: ProcessStage
    hrt_ratio: float = Field(
        default=1.0,
        ge=0.25,
        le=2.5,
        description="HRT 비율 (1.0 = 설계값 100%)",
    )


class StageResult(BaseModel):
    """Result of a single process stage calculation."""
    stage: ProcessStage
    stage_name_ko: str
    hrt_ratio: float
    hrt_hours: float
    influent: WaterQuality
    effluent: WaterQuality
    removal_efficiencies: dict[str, float]
    status: SensorStatus  # worst-case status for this stage


class PipelineParams(BaseModel):
    """Request body for POST /api/pipeline/params."""
    params: list[StageParams]


class PipelineResult(BaseModel):
    """Full pipeline calculation result."""
    raw_water: WaterQuality
    stages: list[StageResult]
    treated_water: WaterQuality
    overall_removal: dict[str, float]  # % removal for each metric
    overall_status: SensorStatus
