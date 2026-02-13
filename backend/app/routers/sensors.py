"""GET /api/sensors — current sensor readings."""

from fastapi import APIRouter

from ..models import SensorResponse
from ..simulator import simulator

router = APIRouter()


@router.get("/sensors", response_model=SensorResponse)
def get_sensors():
    """Return current readings for all 4 sensors."""
    return SensorResponse(sensors=simulator.get_all_sensors())
