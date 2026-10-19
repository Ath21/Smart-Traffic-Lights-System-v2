
import asyncio
import json

import aio_pika
from pydantic import ValidationError
from redis.asyncio import Redis

from app.cache import REDIS_URL, cache_observation
from app.messaging.config import (
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


async def handle_message(
    message: aio_pika.abc.AbstractIncomingMessage,
    exchange: aio_pika.abc.AbstractExchange,
    redis_client: Redis,
) -> None:
    try:
        payload = json.loads(message.body)
        observation = SensorObservation.model_validate(payload)
    except (
        json.JSONDecodeError,
        UnicodeDecodeError,
        ValidationError,
    ):
        # Invalid messages should not be retried indefinitely.
        await message.reject(requeue=False)
        return

    processed = ProcessedSensorObservation(
        **observation.model_dump(),
    )

    # If caching fails, request redelivery rather than silently
    # continuing without the expected cache update.
    try:
        await cache_observation(redis_client, processed)
    except Exception:
        await message.nack(requeue=True)
        raise

    # Publish even when the cache reports "duplicate" or "stale".
    # The cache is a latest-value view; it is not an event-delivery log.
    try:
        await publish_processed(exchange, processed)
    except Exception:
        await message.nack(requeue=True)
        raise

    # Acknowledge only after caching and output publication succeed.
    await message.ack()


async def run_consumer() -> None:
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

            print(
                "Sensor Data Processor is consuming messages "
                "with Redis caching enabled."
            )

            await asyncio.Future()
    finally:
        await redis_client.aclose()


if __name__ == "__main__":
    asyncio.run(run_consumer())


