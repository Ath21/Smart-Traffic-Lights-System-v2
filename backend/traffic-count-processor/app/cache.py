
import os

from redis.asyncio import Redis
from dotenv import load_dotenv
from app.schemas import ProcessedTrafficCount


TRAFFIC_CACHE_TTL_SECONDS = int(
    os.getenv("TRAFFIC_CACHE_TTL_SECONDS")
)
EVENT_DEDUP_TTL_SECONDS = int(
    os.getenv("EVENT_DEDUP_TTL_SECONDS")
)

REDIS_URL = os.getenv("REDIS_URL")


# One atomic operation:
# 1. Ignore an event_id already processed.
# 2. Remember the event_id for the deduplication period.
# 3. Keep the latest observation only if its timestamp is newer.
_UPDATE_LATEST_SCRIPT = """
if redis.call('EXISTS', KEYS[2]) == 1 then
    return 'duplicate'
end

redis.call('SET', KEYS[2], '1', 'EX', ARGV[3])

local current_timestamp = redis.call('HGET', KEYS[1], 'observed_at_ms')

if current_timestamp and tonumber(ARGV[1]) <= tonumber(current_timestamp) then
    return 'stale'
end

redis.call(
    'HSET', KEYS[1],
    'observed_at_ms', ARGV[1],
    'payload', ARGV[2]
)
redis.call('EXPIRE', KEYS[1], ARGV[4])

return 'updated'
"""


def latest_observation_key(
    observation: ProcessedTrafficCount,
    ) -> str:
    return (
        f"stls:traffic:count:{observation.intersection_id}:"
        f"{observation.traffic_type}"
    )


def event_deduplication_key(event_id: str) -> str:
    return f"stls:traffic:processed-count:{event_id}"


async def cache_observation(
    client: Redis,
    observation: ProcessedTrafficCount,
) -> str:
    """Cache an observation and return updated, stale, or duplicate."""

    timestamp_ms = int(observation.observed_at.timestamp() * 1000)

    result = await client.eval(
        _UPDATE_LATEST_SCRIPT,
        2,
        latest_observation_key(observation),
        event_deduplication_key(str(observation.event_id)),
        timestamp_ms,
        observation.model_dump_json(),
        EVENT_DEDUP_TTL_SECONDS,
        TRAFFIC_CACHE_TTL_SECONDS,
    )

    return result.decode() if isinstance(result, bytes) else result

