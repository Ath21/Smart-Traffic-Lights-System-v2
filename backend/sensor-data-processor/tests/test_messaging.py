
import json
from unittest.mock import AsyncMock
from uuid import uuid4

import aio_pika
import pytest

from app.config import INTERSECTION_LIGHTS
from app.messaging.consumer import handle_message
from app.messaging.publisher import publish_processed
from app.schemas import ProcessedSensorObservation


TEST_INTERSECTION_ID = next(iter(INTERSECTION_LIGHTS))
TEST_LIGHT_ID = next(iter(INTERSECTION_LIGHTS[TEST_INTERSECTION_ID]))
TEST_SENSOR_TYPE = "vehicle-count"


def valid_payload() -> dict:
    return {
        "schema_version": "1.0",
        "event_id": str(uuid4()),
        "intersection_id": TEST_INTERSECTION_ID,
        "light_id": TEST_LIGHT_ID,
        "sensor_type": TEST_SENSOR_TYPE,
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
        f"stls.sensor-data.processed."
        f"{TEST_INTERSECTION_ID}.{TEST_SENSOR_TYPE}"
    )
    assert args["mandatory"] is True

    message = exchange.publish.await_args.args[0]
    assert message.content_type == "application/json"
    assert message.message_id == str(observation.event_id)
    assert message.delivery_mode == aio_pika.DeliveryMode.PERSISTENT

    published = json.loads(message.body)
    assert published["count"] == 12
    assert published["intersection_id"] == TEST_INTERSECTION_ID
    assert published["light_id"] == TEST_LIGHT_ID
    assert "processed_at" in published


@pytest.mark.asyncio
async def test_valid_message_is_cached_published_then_acknowledged(monkeypatch):
    payload = valid_payload()
    message = AsyncMock()
    message.body = json.dumps(payload).encode("utf-8")
    exchange = AsyncMock()
    redis_client = AsyncMock()
    cache_mock = AsyncMock(return_value="updated")

    monkeypatch.setattr(
        "app.messaging.consumer.cache_observation",
        cache_mock,
    )

    await handle_message(message, exchange, redis_client)

    cache_mock.assert_awaited_once()
    cached_observation = cache_mock.await_args.args[1]

    assert isinstance(cached_observation, ProcessedSensorObservation)
    assert cached_observation.count == 12
    assert cached_observation.light_id == TEST_LIGHT_ID

    exchange.publish.assert_awaited_once()
    message.ack.assert_awaited_once()
    message.reject.assert_not_awaited()
    message.nack.assert_not_awaited()


@pytest.mark.asyncio
async def test_invalid_message_is_rejected(monkeypatch):
    payload = valid_payload()
    payload["count"] = -1

    message = AsyncMock()
    message.body = json.dumps(payload).encode("utf-8")
    exchange = AsyncMock()
    redis_client = AsyncMock()
    cache_mock = AsyncMock()

    monkeypatch.setattr(
        "app.messaging.consumer.cache_observation",
        cache_mock,
    )

    await handle_message(message, exchange, redis_client)

    cache_mock.assert_not_awaited()
    exchange.publish.assert_not_awaited()
    message.reject.assert_awaited_once_with(requeue=False)
    message.ack.assert_not_awaited()


@pytest.mark.asyncio
async def test_redis_failure_requests_redelivery(monkeypatch):
    message = AsyncMock()
    message.body = json.dumps(valid_payload()).encode("utf-8")
    exchange = AsyncMock()
    redis_client = AsyncMock()
    cache_mock = AsyncMock(
        side_effect=RuntimeError("Redis unavailable"),
    )

    monkeypatch.setattr(
        "app.messaging.consumer.cache_observation",
        cache_mock,
    )

    with pytest.raises(RuntimeError, match="Redis unavailable"):
        await handle_message(message, exchange, redis_client)

    exchange.publish.assert_not_awaited()
    message.nack.assert_awaited_once_with(requeue=True)
    message.ack.assert_not_awaited()


@pytest.mark.asyncio
async def test_publish_failure_requests_redelivery(monkeypatch):
    message = AsyncMock()
    message.body = json.dumps(valid_payload()).encode("utf-8")
    exchange = AsyncMock()
    exchange.publish.side_effect = RuntimeError("broker unavailable")
    redis_client = AsyncMock()
    cache_mock = AsyncMock(return_value="updated")

    monkeypatch.setattr(
        "app.messaging.consumer.cache_observation",
        cache_mock,
    )

    with pytest.raises(RuntimeError, match="broker unavailable"):
        await handle_message(message, exchange, redis_client)

    cache_mock.assert_awaited_once()
    message.nack.assert_awaited_once_with(requeue=True)
    message.ack.assert_not_awaited()
