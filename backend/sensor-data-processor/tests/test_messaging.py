
import json
from unittest.mock import AsyncMock
from uuid import uuid4

import aio_pika
import pytest

from app.messaging.consumer import handle_message
from app.messaging.publisher import publish_processed
from app.schemas import ProcessedSensorObservation


def valid_payload() -> dict:
    return {
        "schema_version": "1.0",
        "event_id": str(uuid4()),
        "intersection_id": "ekklisia-3",
        "sensor_type": "vehicle-count",
        "sensor_id": "sensor-ekklisia-3-vehicle-01",
        "count": 12,
        "observed_at": "2026-10-19T10:15:30Z",
    }


@pytest.mark.asyncio
async def test_publisher_uses_expected_routing_key():
    exchange = AsyncMock()
    observation = ProcessedSensorObservation.model_validate(valid_payload())

    await publish_processed(exchange, observation)

    exchange.publish.assert_awaited_once()
    args = exchange.publish.await_args.kwargs

    assert args["routing_key"] == (
        "stls.sensor-data.processed.ekklisia-3.vehicle-count"
    )
    assert args["mandatory"] is True

    message = exchange.publish.await_args.args[0]
    assert message.content_type == "application/json"
    assert message.message_id == str(observation.event_id)
    assert message.delivery_mode == aio_pika.DeliveryMode.PERSISTENT

    published = json.loads(message.body)
    assert published["count"] == 12
    assert published["intersection_id"] == "ekklisia-3"
    assert "processed_at" in published


@pytest.mark.asyncio
async def test_valid_message_is_published_then_acknowledged():
    payload = valid_payload()

    message = AsyncMock()
    message.body = json.dumps(payload).encode("utf-8")
    exchange = AsyncMock()

    await handle_message(message, exchange)

    exchange.publish.assert_awaited_once()
    message.ack.assert_awaited_once()
    message.reject.assert_not_awaited()
    message.nack.assert_not_awaited()


@pytest.mark.asyncio
async def test_invalid_message_is_rejected():
    payload = valid_payload()
    payload["count"] = -1

    message = AsyncMock()
    message.body = json.dumps(payload).encode("utf-8")
    exchange = AsyncMock()

    await handle_message(message, exchange)

    exchange.publish.assert_not_awaited()
    message.reject.assert_awaited_once_with(requeue=False)
    message.ack.assert_not_awaited()


@pytest.mark.asyncio
async def test_publish_failure_requests_redelivery():
    message = AsyncMock()
    message.body = json.dumps(valid_payload()).encode("utf-8")

    exchange = AsyncMock()
    exchange.publish.side_effect = RuntimeError("broker unavailable")

    with pytest.raises(RuntimeError, match="broker unavailable"):
        await handle_message(message, exchange)

    message.nack.assert_awaited_once_with(requeue=True)
    message.ack.assert_not_awaited()

