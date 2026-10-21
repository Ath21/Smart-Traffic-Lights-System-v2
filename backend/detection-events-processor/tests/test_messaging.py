import json
from unittest.mock import AsyncMock
from uuid import uuid4

import aio_pika
import pytest

from app.config import INTERSECTION_LIGHTS
from app.messaging.consumer import handle_detection_message
from app.messaging.publisher import publish_processed_event
from app.schemas import ProcessedDetectionEvent


TEST_INTERSECTION_ID = next(iter(INTERSECTION_LIGHTS))
TEST_LIGHT_ID = next(iter(INTERSECTION_LIGHTS[TEST_INTERSECTION_ID]))
TEST_DETECTION_TYPE = "emergency-vehicle"


def valid_payload() -> dict:
    return {
        "schema_version": "1.0",
        "event_id": str(uuid4()),
        "intersection_id": TEST_INTERSECTION_ID,
        "light_id": TEST_LIGHT_ID,
        "detection_type": TEST_DETECTION_TYPE,
        "detected_at": "2026-10-21T10:15:30Z",
    }


@pytest.mark.asyncio
async def test_publisher_uses_expected_routing_key():
    exchange = AsyncMock()
    event = ProcessedDetectionEvent.model_validate(valid_payload())

    await publish_processed_event(exchange, event)

    exchange.publish.assert_awaited_once()
    kwargs = exchange.publish.await_args.kwargs

    assert kwargs["routing_key"] == (
        f"stls.detection-events.processed."
        f"{TEST_INTERSECTION_ID}.{TEST_DETECTION_TYPE}"
    )
    assert kwargs["mandatory"] is True

    message = exchange.publish.await_args.args[0]
    assert message.content_type == "application/json"
    assert message.message_id == str(event.event_id)
    assert message.delivery_mode == aio_pika.DeliveryMode.PERSISTENT

    published = json.loads(message.body)
    assert published["event_id"] == str(event.event_id)
    assert published["intersection_id"] == TEST_INTERSECTION_ID
    assert published["light_id"] == TEST_LIGHT_ID
    assert published["detection_type"] == TEST_DETECTION_TYPE
    assert "processed_at" in published


@pytest.mark.asyncio
async def test_valid_message_is_cached_published_then_acknowledged(
    monkeypatch,
):
    payload = valid_payload()
    message = AsyncMock()
    message.body = json.dumps(payload).encode("utf-8")
    exchange = AsyncMock()
    redis_client = AsyncMock()

    cache_mock = AsyncMock(return_value="stored")
    publish_mock = AsyncMock()

    monkeypatch.setattr(
        "app.messaging.consumer.cache_detection_event",
        cache_mock,
    )
    monkeypatch.setattr(
        "app.messaging.consumer.publish_processed_event",
        publish_mock,
    )

    await handle_detection_message(message, exchange, redis_client)

    cache_mock.assert_awaited_once()
    cached_event = cache_mock.await_args.args[1]

    assert isinstance(cached_event, ProcessedDetectionEvent)
    assert str(cached_event.event_id) == payload["event_id"]
    assert cached_event.detection_type == TEST_DETECTION_TYPE
    assert cached_event.light_id == TEST_LIGHT_ID

    publish_mock.assert_awaited_once_with(exchange, cached_event)
    message.ack.assert_awaited_once()
    message.reject.assert_not_awaited()
    message.nack.assert_not_awaited()


@pytest.mark.asyncio
async def test_invalid_message_is_rejected_without_requeue(monkeypatch):
    payload = valid_payload()
    payload["detection_type"] = "unknown-event"

    message = AsyncMock()
    message.body = json.dumps(payload).encode("utf-8")
    exchange = AsyncMock()
    redis_client = AsyncMock()

    cache_mock = AsyncMock()
    publish_mock = AsyncMock()

    monkeypatch.setattr(
        "app.messaging.consumer.cache_detection_event",
        cache_mock,
    )
    monkeypatch.setattr(
        "app.messaging.consumer.publish_processed_event",
        publish_mock,
    )

    await handle_detection_message(message, exchange, redis_client)

    cache_mock.assert_not_awaited()
    publish_mock.assert_not_awaited()
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
    publish_mock = AsyncMock()

    monkeypatch.setattr(
        "app.messaging.consumer.cache_detection_event",
        cache_mock,
    )
    monkeypatch.setattr(
        "app.messaging.consumer.publish_processed_event",
        publish_mock,
    )

    await handle_detection_message(message, exchange, redis_client)

    publish_mock.assert_not_awaited()
    message.nack.assert_awaited_once_with(requeue=True)
    message.ack.assert_not_awaited()


@pytest.mark.asyncio
async def test_publish_failure_requests_redelivery(monkeypatch):
    message = AsyncMock()
    message.body = json.dumps(valid_payload()).encode("utf-8")
    exchange = AsyncMock()
    redis_client = AsyncMock()

    cache_mock = AsyncMock(return_value="stored")
    publish_mock = AsyncMock(
        side_effect=RuntimeError("Publish failed"),
    )

    monkeypatch.setattr(
        "app.messaging.consumer.cache_detection_event",
        cache_mock,
    )
    monkeypatch.setattr(
        "app.messaging.consumer.publish_processed_event",
        publish_mock,
    )

    await handle_detection_message(message, exchange, redis_client)

    cache_mock.assert_awaited_once()
    publish_mock.assert_awaited_once()
    message.nack.assert_awaited_once_with(requeue=True)
    message.ack.assert_not_awaited()


@pytest.mark.asyncio
async def test_duplicate_cached_event_is_republished(monkeypatch):
    payload = valid_payload()
    message = AsyncMock()
    message.body = json.dumps(payload).encode("utf-8")
    exchange = AsyncMock()
    redis_client = AsyncMock()

    cache_mock = AsyncMock(return_value="duplicate")
    publish_mock = AsyncMock()

    monkeypatch.setattr(
        "app.messaging.consumer.cache_detection_event",
        cache_mock,
    )
    monkeypatch.setattr(
        "app.messaging.consumer.publish_processed_event",
        publish_mock,
    )

    await handle_detection_message(message, exchange, redis_client)

    # A prior attempt may have cached the event but failed before publishing.
    publish_mock.assert_awaited_once()
    message.ack.assert_awaited_once()
    message.nack.assert_not_awaited()
