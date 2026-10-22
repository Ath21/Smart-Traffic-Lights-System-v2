
from fastapi import APIRouter, Response, status

router = APIRouter()

_dependencies_ready = False


def set_ready(ready: bool) -> None:
    """Update whether the service is ready to process messages."""
    global _dependencies_ready
    _dependencies_ready = ready


@router.get("/health")
async def health() -> dict[str, str]:
    """Check whether the application process is responsive."""
    return {"status": "healthy"}


@router.get("/ready")
async def readiness(response: Response) -> dict[str, str]:
    """Check whether the service is ready to process messages."""
    if not _dependencies_ready:
        response.status_code = status.HTTP_503_SERVICE_UNAVAILABLE
        return {"status": "not_ready"}

    return {"status": "ready"}
