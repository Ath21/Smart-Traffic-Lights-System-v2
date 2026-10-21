import json
from unittest.mock import AsyncMock

import pytest

from detection_event_generator_common import (
    DetectionEventGenerator,
    IntersectionConfig,
)
from detection_event_generator import publish_detection_events


@pytest.mark.asyncio
async def test_publishes_events_with_expected_routing_keys():
    exchange = AsyncMock()
    intersections = (
        IntersectionConfig(
            "ekklisia-3",
            ("dimitsanas-301S",),
        ),
    )
    generator = DetectionEventGenerator(profile="overload", seed=42)

    count = await publish_detection_events(
        exchange,
        generator,
        intersections,
    )

    assert 10 <= count <= 15
    assert exchange.publish.await_count == count

    for call in exchange.publish.await_args_list:
        message = call.args[0]
        routing_key = call.kwargs["routing_key"]
        payload = json.loads(message.body)

        assert routing_key == (
            "stls.detection-events.input."
            f"{payload['intersection_id']}.{payload['detection_type']}"
        )
        assert payload["intersection_id"] == "ekklisia-3"
        assert payload["light_id"] == "dimitsanas-301S"
        assert payload["schema_version"] == "1.0"
        assert call.kwargs["mandatory"] is True
        assert message.message_id == payload["event_id"]
