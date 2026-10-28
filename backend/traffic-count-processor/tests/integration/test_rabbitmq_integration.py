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
    REDIS_URL,
    cache_observation,
    event_deduplication_key,
    latest_observation_key,
)
from app.config import (
    EXCHANGE_NAME,
    OUTPUT_ROUTING_KEY_TEMPLATE,
    RABBITMQ_URL,
)
from app.messaging.consumer import handle_message
from app.messaging.publisher import publish_processed
from app.schemas import ProcessedTrafficCount, TrafficCount


TZ = ZoneInfo("Europe/Athens")


def make_observation() -> ProcessedTrafficCount:
    return ProcessedTrafficCount(
        schema_version="1.0",
        event_id=uuid4(),
        intersection_id=os.environ["RABBITMQ_TEST_INTERSECTION_ID"],
        light_id=os.environ["RABBITMQ_TEST_LIGHT_ID"],
        traffic_type=os.getenv(
            "RABBITMQ_TEST_TRAFFIC_TYPE", "vehicle-count"
        ),
        count=int(os.getenv("RABBITMQ_TEST_COUNT", "12")),
        observed_at=datetime.now(TZ),
    )


def input_payload(observation: ProcessedTrafficCount) -> dict:
    return TrafficCount(
        **observation.model_dump(exclude={"processed_at"})
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


async def cleanup_event(redis, observation):
    # The latest-observation hash is shared application state; do not delete it.
    await redis.delete(
        event_deduplication_key(str(observation.event_id))
    )


@pytest.mark.asyncio
async def test_publisher_sends_processed_traffic_count_to_rabbitmq():
    observation = make_observation()
    redis, connection = await open_dependencies()
    try:
        async with connection:
            channel = await connection.channel(publisher_confirms=True)
            exchange = await channel.declare_exchange(
                EXCHANGE_NAME,
                type=aio_pika.ExchangeType.TOPIC,
                durable=True,
            )
            routing_key = OUTPUT_ROUTING_KEY_TEMPLATE.format(
                intersection=observation.intersection_id,
                traffic_type=observation.traffic_type,
            )
            queue = await channel.declare_queue(
                exclusive=True, auto_delete=True
            )
            await queue.bind(exchange, routing_key=routing_key)

            await publish_processed(exchange, observation)
            incoming = await receive_one(queue)
            async with incoming.process():
                payload = json.loads(incoming.body)
                assert payload["event_id"] == str(observation.event_id)
                assert payload["traffic_type"] == observation.traffic_type
                assert payload["count"] == observation.count
                assert payload["processed_at"]
                assert incoming.routing_key == routing_key
    finally:
        await cleanup_event(redis, observation)
        await redis.aclose()


@pytest.mark.asyncio
async def test_consumer_processes_rabbitmq_input_and_publishes_output():
    observation = make_observation()
    redis, connection = await open_dependencies()
    try:
        async with connection:
            channel = await connection.channel(publisher_confirms=True)
            exchange = await channel.declare_exchange(
                EXCHANGE_NAME,
                type=aio_pika.ExchangeType.TOPIC,
                durable=True,
            )

            input_key = (
                f"stls.traffic-count.input."
                f"{observation.intersection_id}.{observation.traffic_type}"
            )
            input_queue = await channel.declare_queue(
                exclusive=True, auto_delete=True
            )
            await input_queue.bind(exchange, routing_key=input_key)

            output_key = OUTPUT_ROUTING_KEY_TEMPLATE.format(
                intersection=observation.intersection_id,
                traffic_type=observation.traffic_type,
            )
            output_queue = await channel.declare_queue(
                exclusive=True, auto_delete=True
            )
            await output_queue.bind(exchange, routing_key=output_key)

            async def on_input(message):
                await handle_message(message, exchange, redis)

            await input_queue.consume(on_input)
            await exchange.publish(
                aio_pika.Message(
                    body=json.dumps(input_payload(observation)).encode(),
                    content_type="application/json",
                    message_id=str(observation.event_id),
                    type=observation.traffic_type,
                    delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
                ),
                routing_key=input_key,
                mandatory=True,
            )

            incoming = await receive_one(output_queue)
            async with incoming.process():
                payload = json.loads(incoming.body)
                assert payload["event_id"] == str(observation.event_id)
                assert payload["intersection_id"] == observation.intersection_id
                assert payload["light_id"] == observation.light_id
                assert payload["traffic_type"] == observation.traffic_type
                assert payload["count"] == observation.count
                assert payload["processed_at"]

            cached = await redis.hgetall(latest_observation_key(observation))
            assert cached
            assert "payload" in cached
            assert "observed_at_ms" in cached
            assert json.loads(cached["payload"])["event_id"] == str(
                observation.event_id
            )
            assert await redis.exists(
                event_deduplication_key(str(observation.event_id))
            ) == 1
    finally:
        await cleanup_event(redis, observation)
        await redis.aclose()


@pytest.mark.asyncio
async def test_redis_persists_traffic_count_and_deduplicates_event_id():
    observation = make_observation()
    redis = Redis.from_url(REDIS_URL, decode_responses=True)
    try:
        await redis.ping()
        assert await cache_observation(redis, observation) == "updated"
        assert await cache_observation(redis, observation) == "duplicate"

        cached = await redis.hgetall(latest_observation_key(observation))
        assert cached
        assert "payload" in cached
        assert "observed_at_ms" in cached

        stored = json.loads(cached["payload"])
        assert stored["event_id"] == str(observation.event_id)
        assert stored["traffic_type"] == observation.traffic_type
        assert stored["count"] == observation.count
        assert await redis.exists(
            event_deduplication_key(str(observation.event_id))
        ) == 1
    finally:
        await cleanup_event(redis, observation)
        await redis.aclose()
