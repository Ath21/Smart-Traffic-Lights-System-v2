from uuid import uuid4

import pytest
import pytest_asyncio
from redis.asyncio import Redis

from app.cache import (
    REDIS_URL,
    cache_detection_event,
    detection_event_key,
    event_deduplication_key,
)
from app.config import INTERSECTION_LIGHTS
from app.schemas import ProcessedDetectionEvent

TEST_INTERSECTION_ID = next(iter(INTERSECTION_LIGHTS))
TEST_LIGHT_ID = next(iter(INTERSECTION_LIGHTS[TEST_INTERSECTION_ID]))


@pytest_asyncio.fixture
async def redis_client():
    client = Redis.from_url(REDIS_URL, decode_responses=True)

    try:
        await client.ping()
    except Exception as exc:
        await client.aclose()
        pytest.skip(f"Redis is not available: {exc}")

    yield client
    await client.aclose()


def make_event(*, event_id=None, detection_type="emergency-vehicle"):
    return ProcessedDetectionEvent(
        event_id=event_id or uuid4(),
        intersection_id=TEST_INTERSECTION_ID,
        light_id=TEST_LIGHT_ID,
        detection_type=detection_type,
        detected_at="2026-10-21T10:15:30Z",
    )


async def clear_test_keys(client, event):
    await client.delete(
        detection_event_key(event),
        event_deduplication_key(str(event.event_id)),
    )


@pytest.mark.asyncio
async def test_new_event_is_cached(redis_client):
    event = make_event()
    await clear_test_keys(redis_client, event)

    try:
        result = await cache_detection_event(redis_client, event)

        assert result == "stored"

        cached = await redis_client.get(detection_event_key(event))
        assert cached is not None
        assert event.model_dump_json() == cached
    finally:
        await clear_test_keys(redis_client, event)


@pytest.mark.asyncio
async def test_duplicate_event_is_not_stored_twice(redis_client):
    event_id = uuid4()
    first = make_event(event_id=event_id)
    duplicate = make_event(event_id=event_id)

    await clear_test_keys(redis_client, first)

    try:
        assert await cache_detection_event(redis_client, first) == "stored"
        assert await cache_detection_event(redis_client, duplicate) == "duplicate"
    finally:
        await clear_test_keys(redis_client, first)


@pytest.mark.asyncio
async def test_distinct_events_are_cached_independently(redis_client):
    first = make_event()
    second = make_event()

    await clear_test_keys(redis_client, first)
    await clear_test_keys(redis_client, second)

    try:
        assert await cache_detection_event(redis_client, first) == "stored"
        assert await cache_detection_event(redis_client, second) == "stored"

        first_cached = await redis_client.get(detection_event_key(first))
        second_cached = await redis_client.get(detection_event_key(second))

        assert first_cached is not None
        assert second_cached is not None
        assert first.event_id != second.event_id
    finally:
        await clear_test_keys(redis_client, first)
        await clear_test_keys(redis_client, second)
