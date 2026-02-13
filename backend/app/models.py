"""Pydantic models for AquaView API responses."""

from __future__ import annotations

from datetime import datetime
from enum import Enum
from pydantic import BaseModel


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
