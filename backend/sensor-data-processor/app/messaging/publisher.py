
import aio_pika

from app.config import OUTPUT_ROUTING_KEY_TEMPLATE
from app.schemas import ProcessedSensorObservation


async def publish_processed(
    exchange: aio_pika.abc.AbstractExchange,
    observation: ProcessedSensorObservation,
) -> None:
    routing_key = OUTPUT_ROUTING_KEY_TEMPLATE.format(
        intersection=observation.intersection_id,
        sensor_type=observation.sensor_type,
    )

    message = aio_pika.Message(
        body=observation.model_dump_json().encode("utf-8"),
        content_type="application/json",
        delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
        message_id=str(observation.event_id),
        type=observation.sensor_type,
    )

    await exchange.publish(
        message,
        routing_key=routing_key,
        mandatory=True,
    )

