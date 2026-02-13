"""GET /api/history — sensor history data."""

from fastapi import APIRouter, Query

from ..models import HistoryResponse, SensorType
from ..simulator import simulator

router = APIRouter()


@router.get("/history", response_model=HistoryResponse)
def get_history(
    sensor: SensorType = Query(..., description="Sensor type: ph, turbidity, flow, temp"),
    limit: int = Query(20, ge=1, le=100, description="Number of recent entries"),
):
    """Return recent history for a specific sensor."""
    return HistoryResponse(**simulator.get_history(sensor, limit))
