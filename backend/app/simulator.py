"""In-memory sensor data simulator for AquaView."""

from __future__ import annotations

import random
from collections import deque
from datetime import datetime, timezone

from .models import SensorStatus, SensorType

# ── Sensor configurations ──────────────────────────────────────────
# Each sensor: (unit, normal_min, normal_max, warn_lo, warn_hi, danger_lo, danger_hi)
# Danger zone: value < danger_lo or value > danger_hi
# Warning zone: danger_lo <= value < warn_lo or warn_hi < value <= danger_hi
# Normal zone: warn_lo <= value <= warn_hi

SENSOR_CONFIG: dict[SensorType, dict] = {
    SensorType.PH: {
        "unit": "pH",
        "range": (4.0, 11.0),       # simulation generation range
        "normal": (6.5, 8.5),
        "warning": (6.0, 9.0),
        # outside warning → danger
    },
    SensorType.TURBIDITY: {
        "unit": "NTU",
        "range": (0.0, 15.0),
        "normal": (0.0, 5.0),
        "warning": (0.0, 10.0),
    },
    SensorType.FLOW: {
        "unit": "m³/h",
        "range": (10.0, 200.0),
        "normal": (50.0, 150.0),
        "warning": (30.0, 180.0),
    },
    SensorType.TEMP: {
        "unit": "°C",
        "range": (5.0, 35.0),
        "normal": (15.0, 25.0),
        "warning": (10.0, 30.0),
    },
}

MAX_HISTORY = 100  # max history entries to keep per sensor


def _classify(value: float, config: dict) -> SensorStatus:
    """Classify a sensor value into normal/warning/danger."""
    n_lo, n_hi = config["normal"]
    w_lo, w_hi = config["warning"]

    if n_lo <= value <= n_hi:
        return SensorStatus.NORMAL
    if w_lo <= value <= w_hi:
        return SensorStatus.WARNING
    return SensorStatus.DANGER


def _generate_value(config: dict, prev_value: float | None = None) -> float:
    """Generate a realistic sensor value with small drift from previous."""
    r_lo, r_hi = config["range"]
    n_lo, n_hi = config["normal"]

    if prev_value is None:
        # First reading: mostly normal with some variance
        base = random.uniform(n_lo, n_hi)
        noise = random.gauss(0, (n_hi - n_lo) * 0.3)
        return round(max(r_lo, min(r_hi, base + noise)), 2)

    # Drift from previous value — biased toward normal range
    drift = random.gauss(0, (n_hi - n_lo) * 0.15)
    # Slight pull toward normal center
    center = (n_lo + n_hi) / 2
    pull = (center - prev_value) * 0.05
    new_val = prev_value + drift + pull

    return round(max(r_lo, min(r_hi, new_val)), 2)


class SensorSimulator:
    """Manages simulated sensor data with history."""

    def __init__(self) -> None:
        # Latest value per sensor
        self._current: dict[SensorType, float] = {}
        # History per sensor (deque for auto-truncation)
        self._history: dict[SensorType, deque] = {
            st: deque(maxlen=MAX_HISTORY) for st in SensorType
        }
        # Generate initial readings
        self._tick()

    def _tick(self) -> None:
        """Generate one new reading for every sensor."""
        now = datetime.now(timezone.utc)
        for sensor_type, config in SENSOR_CONFIG.items():
            prev = self._current.get(sensor_type)
            value = _generate_value(config, prev)
            status = _classify(value, config)
            self._current[sensor_type] = value
            self._history[sensor_type].append({
                "value": value,
                "status": status,
                "timestamp": now,
            })

    def get_all_sensors(self) -> list[dict]:
        """Return current readings for all sensors (generates new tick)."""
        self._tick()
        now = datetime.now(timezone.utc)
        result = []
        for sensor_type, config in SENSOR_CONFIG.items():
            value = self._current[sensor_type]
            result.append({
                "sensor": sensor_type,
                "value": value,
                "unit": config["unit"],
                "status": _classify(value, config),
                "timestamp": now,
            })
        return result

    def get_alerts(self) -> list[dict]:
        """Return alerts for sensors NOT in normal state."""
        sensors = self.get_all_sensors()
        alerts = []
        for s in sensors:
            if s["status"] != SensorStatus.NORMAL:
                config = SENSOR_CONFIG[s["sensor"]]
                n_lo, n_hi = config["normal"]
                msg = (
                    f"{s['sensor'].value} is {s['status'].value}: "
                    f"{s['value']}{config['unit']} "
                    f"(normal: {n_lo}~{n_hi}{config['unit']})"
                )
                alerts.append({
                    "sensor": s["sensor"],
                    "value": s["value"],
                    "unit": config["unit"],
                    "status": s["status"],
                    "message": msg,
                    "timestamp": s["timestamp"],
                })
        return alerts

    def get_history(self, sensor: SensorType, limit: int = 20) -> dict:
        """Return recent history for a specific sensor."""
        config = SENSOR_CONFIG[sensor]
        entries = list(self._history[sensor])[-limit:]
        return {
            "sensor": sensor,
            "unit": config["unit"],
            "data": entries,
        }


# ── Singleton instance ──────────────────────────────────────────────
simulator = SensorSimulator()
