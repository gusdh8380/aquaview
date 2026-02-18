"""AquaView — Water Treatment Process Monitoring API."""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from .routers import alerts, history, pipeline, sensors

app = FastAPI(
    title="AquaView API",
    description="수처리 공정 모니터링 시스템 REST API",
    version="0.1.0",
)

# CORS — React dev server (3000/5173) + Unity WebGL
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # MVP: allow all; tighten in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Register routers
app.include_router(sensors.router, prefix="/api", tags=["sensors"])
app.include_router(alerts.router, prefix="/api", tags=["alerts"])
app.include_router(history.router, prefix="/api", tags=["history"])
app.include_router(pipeline.router, prefix="/api", tags=["pipeline"])


@app.get("/")
def root():
    return {"service": "AquaView API", "status": "running"}
