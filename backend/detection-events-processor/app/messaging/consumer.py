import asyncio
import logging
from collections.abc import Callable

import aio_pika
from pydantic import ValidationError
from redis.asyncio import Redis

from app.cache import cache_detection_event
from app.config import (
    RABBITMQ_EXCHANGE_NAME,
    RABBITMQ_INPUT_QUEUE,
    RABBITMQ_INPUT_ROUTING_KEY,
    RABBITMQ_URL,
    REDIS_URL,
)
from app.messaging.publisher import publish_processed_event
from app.schemas import DetectionEvent, ProcessedDetectionEvent

logger = logging.getLogger(__name__)


async def handle_detection_message(
    message,
    exchange,
    redis_client: Redis,
) -> str:
    """Validate, cache and publish one detection event."""
    try:
        event = DetectionEvent.model_validate_json(message.body)
    except (ValidationError, ValueError):
        logger.warning("Rejecting invalid detection event", exc_info=True)
        await message.reject(requeue=False)
        return "rejected"

    processed_event = ProcessedDetectionEvent.model_validate(
        event.model_dump()
    )

    try:
        cache_result = await cache_detection_event(
            redis_client,
            processed_event,
        )

        # Republish even when cached previously: an earlier attempt may
        # have cached successfully but failed before publishing.
        await publish_processed_event(exchange, processed_event)

    except Exception:
        logger.exception(
            "Failed to process detection event %s",
            processed_event.event_id,
        )
        await message.nack(requeue=True)
        return "requeued"

    await message.ack()

    logger.info(
        "Processed detection event %s (cache result: %s)",
        processed_event.event_id,
        cache_result,
    )
    return "processed"


async def run_consumer(
    readiness_callback: Callable[[bool], None] | None = None,
) -> None:
    """Connect dependencies and consume detection events continuously."""
    redis_client = Redis.from_url(REDIS_URL, decode_responses=True)
    ready = False

    try:
        await redis_client.ping()
        logger.info("Connected to Redis")

        while True:
            try:
                connection = await aio_pika.connect_robust(RABBITMQ_URL)

                async with connection:
                    channel = await connection.channel(
                        publisher_confirms=True,
                    )
                    await channel.set_qos(prefetch_count=10)

                    exchange = await channel.declare_exchange(
                        RABBITMQ_EXCHANGE_NAME,
                        type=aio_pika.ExchangeType.TOPIC,
                        durable=True,
                    )

                    queue = await channel.declare_queue(
                        RABBITMQ_INPUT_QUEUE,
                        durable=True,
                    )

                    await queue.bind(
                        exchange,
                        routing_key=RABBITMQ_INPUT_ROUTING_KEY,
                    )

                    await queue.consume(
                        lambda message: handle_detection_message(
                            message,
                            exchange,
                            redis_client,
                        )
                    )

                    ready = True
                    if readiness_callback is not None:
                        readiness_callback(True)

                    logger.info(
                        "Consuming queue %s on exchange %s "
                        "with binding %s",
                        RABBITMQ_INPUT_QUEUE,
                        RABBITMQ_EXCHANGE_NAME,
                        RABBITMQ_INPUT_ROUTING_KEY,
                    )

                    # Wait until the connection closes or task is cancelled.
                    await asyncio.Future()

            except asyncio.CancelledError:
                raise
            except Exception:
                if ready:
                    ready = False
                    if readiness_callback is not None:
                        readiness_callback(False)

                logger.exception(
                    "RabbitMQ consumer failed; retrying in 5 seconds"
                )
                await asyncio.sleep(5)

    finally:
        if readiness_callback is not None:
            readiness_callback(False)

        await redis_client.aclose()
