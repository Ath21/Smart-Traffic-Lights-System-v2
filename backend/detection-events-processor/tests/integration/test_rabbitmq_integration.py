import asyncio
import json
import os
from datetime import datetime
from pathlib import Path
from uuid import uuid4
from zoneinfo import ZoneInfo

import aio_pika
import pytest
from dotenv import load_dotenv
from redis.asyncio import Redis

SERVICE_ROOT = Path(__file__).resolve().parents[2]
load_dotenv(SERVICE_ROOT / ".env", override=False)

pytestmark = [
    pytest.mark.integration,
    pytest.mark.skipif(
        os.getenv("RUN_RABBITMQ_INTEGRATION") != "1",
        reason="Set RUN_RABBITMQ_INTEGRATION=1 to run integration tests",
    ),
]

from app.cache import (
    cache_detection_event,
    detection_event_key,
    event_deduplication_key,
)
from app.config import (
    RABBITMQ_EXCHANGE_NAME,
    RABBITMQ_INPUT_ROUTING_KEY,
    RABBITMQ_OUTPUT_ROUTING_KEY_TEMPLATE,
    RABBITMQ_URL,
    REDIS_URL,
)
from app.messaging.consumer import handle_detection_message
from app.messaging.publisher import (
    declare_detection_exchange,
    publish_processed_event,
)
from app.schemas import DetectionEvent, ProcessedDetectionEvent


TZ = ZoneInfo("Europe/Athens")


def make_event() -> ProcessedDetectionEvent:
    return ProcessedDetectionEvent(
        schema_version="1.0",
        event_id=uuid4(),
        intersection_id=os.environ["RABBITMQ_TEST_INTERSECTION_ID"],
        light_id=os.environ["RABBITMQ_TEST_LIGHT_ID"],
        detection_type=os.getenv("RABBITMQ_TEST_DETECTION_TYPE", "accident"),
        detected_at=datetime.now(TZ),
    )


def input_payload(event: ProcessedDetectionEvent) -> dict:
    return DetectionEvent(
        **event.model_dump(exclude={"processed_at"})
    ).model_dump(mode="json")


async def receive_one(queue, timeout: float = 10):
    async with asyncio.timeout(timeout):
        async with queue.iterator() as iterator:
            return await anext(iterator)


async def open_dependencies():
    redis = Redis.from_url(REDIS_URL, decode_responses=True)
    await redis.ping()
    connection = await aio_pika.connect_robust(RABBITMQ_URL)
    return redis, connection


async def cleanup_event(redis, event):
    await redis.delete(
        detection_event_key(event),
        event_deduplication_key(str(event.event_id)),
    )


@pytest.mark.asyncio
async def test_publisher_sends_processed_event_to_rabbitmq():
    event = make_event()
    redis, connection = await open_dependencies()
    try:
        async with connection:
            channel = await connection.channel(publisher_confirms=True)
            exchange = await declare_detection_exchange(channel)
            routing_key = RABBITMQ_OUTPUT_ROUTING_KEY_TEMPLATE.format(
                intersection=event.intersection_id,
                detection_type=event.detection_type,
            )
            queue = await channel.declare_queue(
                exclusive=True, auto_delete=True
            )
            await queue.bind(exchange, routing_key=routing_key)

            await publish_processed_event(exchange, event)
            incoming = await receive_one(queue)
            async with incoming.process():
                payload = json.loads(incoming.body)
                assert payload["event_id"] == str(event.event_id)
                assert payload["detection_type"] == event.detection_type
                assert payload["processed_at"]
                assert incoming.routing_key == routing_key
    finally:
        await cleanup_event(redis, event)
        await redis.aclose()


@pytest.mark.asyncio
async def test_consumer_processes_rabbitmq_input_and_publishes_output():
    event = make_event()
    redis, connection = await open_dependencies()
    try:
        async with connection:
            channel = await connection.channel(publisher_confirms=True)
            exchange = await declare_detection_exchange(channel)

            input_queue = await channel.declare_queue(
                exclusive=True, auto_delete=True
            )
            input_key = (
                f"stls.detection-events.input."
                f"{event.intersection_id}.{event.detection_type}"
            )
            await input_queue.bind(exchange, routing_key=input_key)

            output_key = RABBITMQ_OUTPUT_ROUTING_KEY_TEMPLATE.format(
                intersection=event.intersection_id,
                detection_type=event.detection_type,
            )
            output_queue = await channel.declare_queue(
                exclusive=True, auto_delete=True
            )
            await output_queue.bind(exchange, routing_key=output_key)

            async def on_input(message):
                await handle_detection_message(message, exchange, redis)

            await input_queue.consume(on_input)
            await exchange.publish(
                aio_pika.Message(
                    body=json.dumps(input_payload(event)).encode(),
                    content_type="application/json",
                    message_id=str(event.event_id),
                    type=event.detection_type,
                    delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
                ),
                routing_key=input_key,
                mandatory=True,
            )

            incoming = await receive_one(output_queue)
            async with incoming.process():
                payload = json.loads(incoming.body)
                assert payload["event_id"] == str(event.event_id)
                assert payload["intersection_id"] == event.intersection_id
                assert payload["light_id"] == event.light_id
                assert payload["processed_at"]

            cached = await redis.get(detection_event_key(event))
            assert cached is not None
            assert json.loads(cached)["event_id"] == str(event.event_id)
            assert await redis.exists(
                event_deduplication_key(str(event.event_id))
            ) == 1
    finally:
        await cleanup_event(redis, event)
        await redis.aclose()


@pytest.mark.asyncio
async def test_redis_persists_detection_event_and_deduplicates_event_id():
    event = make_event()
    redis = Redis.from_url(REDIS_URL, decode_responses=True)
    try:
        await redis.ping()
        assert await cache_detection_event(redis, event) == "stored"
        assert await cache_detection_event(redis, event) == "duplicate"

        cached = await redis.get(detection_event_key(event))
        assert cached is not None
        assert json.loads(cached)["event_id"] == str(event.event_id)
        assert await redis.exists(
            event_deduplication_key(str(event.event_id))
        ) == 1
    finally:
        await cleanup_event(redis, event)
        await redis.aclose()
