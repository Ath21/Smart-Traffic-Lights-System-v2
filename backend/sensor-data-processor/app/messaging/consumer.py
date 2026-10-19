
import json

import aio_pika
from pydantic import ValidationError

from app.messaging.config import (
    EXCHANGE_NAME,
    EXCHANGE_TYPE,
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
) -> None:
    try:
        payload = json.loads(message.body)
        observation = SensorObservation.model_validate(payload)
    except (json.JSONDecodeError, UnicodeDecodeError, ValidationError):
        # Invalid messages must not be retried forever.
        # A dead-letter queue can be added in the reliability milestone.
        await message.reject(requeue=False)
        return

    processed = ProcessedSensorObservation(
        **observation.model_dump(),
    )

    # If publishing fails, leave the input eligible for redelivery.
    try:
        await publish_processed(exchange, processed)
    except Exception:
        await message.nack(requeue=True)
        raise

    # Acknowledge only after the output publication succeeds.
    await message.ack()


async def run_consumer() -> None:
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
            lambda message: handle_message(message, exchange),
            no_ack=False,
        )

        print("Sensor Data Processor is consuming messages.")

        # Keep the connection open while the consumer runs.
        import asyncio
        await asyncio.Future()

