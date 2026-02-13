"""GET /api/alerts — current alert list."""

from fastapi import APIRouter

from ..models import AlertResponse
from ..simulator import simulator

router = APIRouter()


@router.get("/alerts", response_model=AlertResponse)
def get_alerts():
    """Return alerts for sensors in warning or danger state."""
    return AlertResponse(alerts=simulator.get_alerts())
