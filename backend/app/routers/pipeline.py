"""GET /api/pipeline & POST /api/pipeline/params — pipeline simulation."""

from fastapi import APIRouter

from ..models import PipelineParams, PipelineResult
from ..pipeline import run_pipeline

router = APIRouter()


@router.get("/pipeline", response_model=PipelineResult)
def get_pipeline():
    """Return pipeline result with all HRT at design value (ratio=1.0)."""
    return run_pipeline()


@router.post("/pipeline/params", response_model=PipelineResult)
def post_pipeline_params(body: PipelineParams):
    """
    Recalculate pipeline with given HRT ratios.

    Each stage param has:
    - stage: 'primary_settling' | 'aeration' | 'secondary_settling' | 'nitrification' | 'disinfection'
    - hrt_ratio: 0.25–2.5 (1.0 = design HRT 100%)
    """
    return run_pipeline(body.params)
