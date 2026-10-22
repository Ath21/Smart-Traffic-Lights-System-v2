
from datetime import datetime, timedelta, timezone
from uuid import uuid4

import pytest
import pytest_asyncio
from redis.asyncio import Redis

from app.cache import (
    REDIS_URL,
    cache_observation,
    event_deduplication_key,
    latest_observation_key,
)
from app.config import INTERSECTION_LIGHTS
from app.schemas import ProcessedSensorObservation


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


def make_observation(
    *,
    event_id=None,
    count: int = 10,
    observed_at: datetime | None = None,
    intersection_id: str = TEST_INTERSECTION_ID,
    light_id: str = TEST_LIGHT_ID,
    sensor_type: str = "vehicle-count",
) -> ProcessedSensorObservation:
    return ProcessedSensorObservation(
        event_id=event_id or uuid4(),
        intersection_id=intersection_id,
        light_id=light_id,
        sensor_type=sensor_type,
        count=count,
        observed_at=observed_at or datetime.now(timezone.utc),
    )


async def clear_test_keys(
    client: Redis,
    observation: ProcessedSensorObservation,
) -> None:
    await client.delete(
        latest_observation_key(observation),
        event_deduplication_key(str(observation.event_id)),
    )


@pytest.mark.asyncio
async def test_new_observation_updates_latest_cache(redis_client):
    observation = make_observation()
    await clear_test_keys(redis_client, observation)

    try:
        result = await cache_observation(redis_client, observation)

        assert result == "updated"

        cached = await redis_client.hgetall(
            latest_observation_key(observation)
        )
        assert cached["payload"] == observation.model_dump_json()
        assert int(cached["observed_at_ms"]) == int(
            observation.observed_at.timestamp() * 1000
        )
    finally:
        await clear_test_keys(redis_client, observation)


@pytest.mark.asyncio
async def test_duplicate_event_does_not_update_cache(redis_client):
    event_id = uuid4()
    first = make_observation(
        event_id=event_id,
        count=10,
        observed_at=datetime(2026, 10, 19, 10, 0, tzinfo=timezone.utc),
    )
    duplicate = make_observation(
        event_id=event_id,
        count=99,
        observed_at=datetime(2026, 10, 19, 10, 1, tzinfo=timezone.utc),
    )
    await clear_test_keys(redis_client, first)

    try:
        assert await cache_observation(redis_client, first) == "updated"
        assert await cache_observation(redis_client, duplicate) == "duplicate"

        cached = await redis_client.hgetall(latest_observation_key(first))
        assert '"count":10' in cached["payload"]
    finally:
        await clear_test_keys(redis_client, first)


@pytest.mark.asyncio
async def test_older_observation_does_not_overwrite_newer(redis_client):
    now = datetime.now(timezone.utc)
    newer = make_observation(count=20, observed_at=now)
    older = make_observation(
        count=5,
        observed_at=now - timedelta(seconds=30),
    )
    await clear_test_keys(redis_client, newer)
    await clear_test_keys(redis_client, older)

    try:
        assert await cache_observation(redis_client, newer) == "updated"
        assert await cache_observation(redis_client, older) == "stale"

        cached = await redis_client.hgetall(latest_observation_key(newer))
        assert '"count":20' in cached["payload"]
    finally:
        await clear_test_keys(redis_client, newer)
        await clear_test_keys(redis_client, older)
