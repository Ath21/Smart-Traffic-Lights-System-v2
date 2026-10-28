from redis.asyncio import Redis

from app.config import (
    DETECTION_EVENT_CACHE_TTL_SECONDS,
    EVENT_DEDUP_TTL_SECONDS,
    REDIS_URL,
)
from app.schemas import ProcessedDetectionEvent


_CACHE_EVENT_SCRIPT = """
if redis.call('EXISTS', KEYS[2]) == 1 then
    return 'duplicate'
end

redis.call('SET', KEYS[2], '1', 'EX', ARGV[2])
redis.call('SET', KEYS[1], ARGV[1], 'EX', ARGV[3])

return 'stored'
"""


def detection_event_key(event: ProcessedDetectionEvent) -> str:
    return f"stls:detection:events:{event.event_id}"


def event_deduplication_key(event_id: str) -> str:
    return f"stls:detection:processed-events:{event_id}"


async def cache_detection_event(
    client: Redis,
    event: ProcessedDetectionEvent,
) -> str:
    result = await client.eval(
        _CACHE_EVENT_SCRIPT,
        2,
        detection_event_key(event),
        event_deduplication_key(str(event.event_id)),
        event.model_dump_json(),
        EVENT_DEDUP_TTL_SECONDS,
        DETECTION_EVENT_CACHE_TTL_SECONDS,
    )

    return result.decode() if isinstance(result, bytes) else result
