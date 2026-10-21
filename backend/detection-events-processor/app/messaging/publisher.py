import aio_pika
from aio_pika.abc import AbstractExchange

from app.config import (
    RABBITMQ_EXCHANGE_NAME,
    RABBITMQ_OUTPUT_ROUTING_KEY_TEMPLATE,
)
from app.schemas import ProcessedDetectionEvent


async def declare_detection_exchange(channel):
    """Declare the durable topic exchange used for detection events."""
    return await channel.declare_exchange(
        RABBITMQ_EXCHANGE_NAME,
        type=aio_pika.ExchangeType.TOPIC,
        durable=True,
    )


async def publish_processed_event(
    exchange: AbstractExchange,
    event: ProcessedDetectionEvent,
) -> None:
    """Publish a processed detection event as a persistent message."""
    routing_key = RABBITMQ_OUTPUT_ROUTING_KEY_TEMPLATE.format(
        intersection=event.intersection_id,
        detection_type=event.detection_type,
    )

    message = aio_pika.Message(
        body=event.model_dump_json().encode("utf-8"),
        content_type="application/json",
        message_id=str(event.event_id),
        type=event.detection_type,
        delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
    )

    await exchange.publish(
        message,
        routing_key=routing_key,
        mandatory=True,
    )


# Compatibility alias for any existing code using the earlier name.
publish_processed_detection = publish_processed_event
