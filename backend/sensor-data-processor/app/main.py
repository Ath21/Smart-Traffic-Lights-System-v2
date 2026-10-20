
import asyncio
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.api.health import router as health_router, set_ready
from app.messaging.consumer import run_consumer

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s: %(message)s",
)

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    set_ready(False)

    consumer_task = asyncio.create_task(
        run_consumer(readiness_callback=set_ready),
        name="sensor-data-consumer",
    )

    logger.info("Starting Sensor Data Processor")

    try:
        yield
    finally:
        set_ready(False)
        consumer_task.cancel()

        try:
            await consumer_task
        except asyncio.CancelledError:
            pass

        logger.info("Sensor Data Processor stopped")


app = FastAPI(
    title="Sensor Data Processor",
    description=(
        "Validates, deduplicates, caches, and publishes "
        "sensor observations."
    ),
    version="1.0",
    lifespan=lifespan,
)

app.include_router(health_router)
