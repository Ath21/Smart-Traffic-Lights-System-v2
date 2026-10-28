
import logging

import aio_pika

from app.config import OUTPUT_ROUTING_KEY_TEMPLATE
from app.schemas import ProcessedTrafficCount

logger = logging.getLogger(__name__)


async def publish_processed(
    exchange: aio_pika.abc.AbstractExchange,
    observation: ProcessedTrafficCount,
) -> None:
    routing_key = OUTPUT_ROUTING_KEY_TEMPLATE.format(
        intersection=observation.intersection_id,
        traffic_type=observation.traffic_type,
    )

    message = aio_pika.Message(
        body=observation.model_dump_json().encode("utf-8"),
        content_type="application/json",
        delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
        message_id=str(observation.event_id),
        type=observation.traffic_type,
    )

    try:
        await exchange.publish(
            message,
            routing_key=routing_key,
            mandatory=True,
        )
    except Exception:
        logger.exception(
            "Failed to publish processed event: "
            "event_id=%s routing_key=%s",
            observation.event_id,
            routing_key,
        )
        raise

    logger.info(
        "Processed event published: event_id=%s "
        "intersection=%s light=%s traffic_type=%s "
        "count=%s routing_key=%s",
        observation.event_id,
        observation.intersection_id,
        observation.light_id,
        observation.traffic_type,
        observation.count,
        routing_key,
    )
