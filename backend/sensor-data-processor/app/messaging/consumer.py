
import asyncio
import json
import logging
from collections.abc import Callable

import aio_pika
from pydantic import ValidationError
from redis.asyncio import Redis

from app.cache import REDIS_URL, cache_observation
from app.config import (
    EXCHANGE_NAME,
    INPUT_QUEUE,
    INPUT_ROUTING_KEY,
    RABBITMQ_URL,
)
from app.messaging.publisher import publish_processed
from app.schemas import (
    ProcessedSensorObservation,
    SensorObservation,
)

logger = logging.getLogger(__name__)


async def handle_message(
    message: aio_pika.abc.AbstractIncomingMessage,
    exchange: aio_pika.abc.AbstractExchange,
    redis_client: Redis,
) -> None:
    event_id = message.message_id or "unknown"

    try:
        payload = json.loads(message.body)
        observation = SensorObservation.model_validate(payload)

    except (
        json.JSONDecodeError,
        UnicodeDecodeError,
        ValidationError,
    ) as exc:
        logger.warning(
            "Rejecting invalid sensor message: event_id=%s error=%s",
            event_id,
            exc,
        )
        await message.reject(requeue=False)
        return

    processed = ProcessedSensorObservation(
        **observation.model_dump(),
    )

    try:
        await cache_observation(redis_client, processed)
    except Exception:
        logger.exception(
            "Redis cache update failed: event_id=%s",
            processed.event_id,
        )
        await message.nack(requeue=True)
        return

    try:
        await publish_processed(exchange, processed)
    except Exception:
        logger.exception(
            "Processed event publication failed: event_id=%s",
            processed.event_id,
        )
        await message.nack(requeue=True)
        return

    await message.ack()

    logger.info(
        "Sensor event processed: event_id=%s intersection=%s "
        "light=%s sensor_type=%s count=%s",
        processed.event_id,
        processed.intersection_id,
        processed.light_id,
        processed.sensor_type,
        processed.count,
    )


async def run_consumer(
    readiness_callback: Callable[[bool], None] | None = None,
) -> None:
    def update_readiness(ready: bool) -> None:
        if readiness_callback is not None:
            readiness_callback(ready)

    update_readiness(False)

    redis_client = Redis.from_url(
        REDIS_URL,
        decode_responses=True,
    )

    try:
        await redis_client.ping()

        connection = await aio_pika.connect_robust(RABBITMQ_URL)

        async with connection:
            channel = await connection.channel(
                publisher_confirms=True,
            )
            await channel.set_qos(prefetch_count=10)

            exchange = await channel.declare_exchange(
                EXCHANGE_NAME,
                aio_pika.ExchangeType.TOPIC,
                durable=True,
            )

            queue = await channel.declare_queue(
                INPUT_QUEUE,
                durable=True,
            )

            await queue.bind(
                exchange,
                routing_key=INPUT_ROUTING_KEY,
            )

            await queue.consume(
                lambda message: handle_message(
                    message,
                    exchange,
                    redis_client,
                ),
                no_ack=False,
            )

            logger.info(
                "Sensor Data Processor started: "
                "exchange=%s queue=%s routing_key=%s",
                EXCHANGE_NAME,
                INPUT_QUEUE,
                INPUT_ROUTING_KEY,
            )

            update_readiness(True)

            while True:
                await asyncio.sleep(5)

                try:
                    await redis_client.ping()

                    broker_ready = (
                        not connection.is_closed
                        and connection.connected.is_set()
                    )
                    update_readiness(broker_ready)

                except Exception:
                    logger.exception(
                        "Dependency health check failed"
                    )
                    update_readiness(False)

    finally:
        update_readiness(False)
        await redis_client.aclose()


if __name__ == "__main__":
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s %(levelname)s %(name)s: %(message)s",
    )
    asyncio.run(run_consumer())
