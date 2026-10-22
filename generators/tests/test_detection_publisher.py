import json
from datetime import datetime
from unittest.mock import AsyncMock
from uuid import UUID
from zoneinfo import ZoneInfo

import pytest

from detection_event_generator_common import (
    DetectionEvent,
    DetectionEventGenerator,
    load_detection_dataset,
)
from detection_event_generator import publish_detection_events

ATHENS_TZ = ZoneInfo("Europe/Athens")
TIMESTAMP = datetime(2026, 10, 22, 8, 0, tzinfo=ATHENS_TZ)


@pytest.mark.asyncio
async def test_publishes_events_with_expected_routing_keys(monkeypatch):
    exchange = AsyncMock()
    dataset = load_detection_dataset()

    generator = DetectionEventGenerator(
        dataset=dataset,
        interval_seconds=30,
        seed=42,
    )

    event = DetectionEvent(
        event_id=UUID("00000000-0000-4000-8000-000000000001"),
        intersection_id="ekklisia-3",
        light_id="dimitsanas-301S",
        detection_type="accident",
        detected_at=TIMESTAMP,
    )

    monkeypatch.setattr(
        generator,
        "generate_events",
        lambda detected_at: [event],
    )

    count = await publish_detection_events(exchange, generator, TIMESTAMP)

    assert count == 1
    exchange.publish.assert_awaited_once()

    call = exchange.publish.await_args
    message = call.args[0]
    payload = json.loads(message.body)

    assert call.kwargs["routing_key"] == (
        "stls.detection-events.input.ekklisia-3.accident"
    )
    assert call.kwargs["mandatory"] is True
    assert message.content_type == "application/json"
    assert message.message_id == str(event.event_id)
    assert payload["intersection_id"] == "ekklisia-3"
    assert payload["light_id"] == "dimitsanas-301S"
    assert payload["detection_type"] == "accident"
    assert payload["schema_version"] == "1.0"
