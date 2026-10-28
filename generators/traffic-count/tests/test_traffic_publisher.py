import json
from datetime import datetime
from unittest.mock import AsyncMock
from zoneinfo import ZoneInfo

import pytest

from traffic_count_generator_common import (
    SENSOR_TYPES,
    SensorDataGenerator,
    load_traffic_dataset,
)
from traffic_count_generator import publish_observations

ATHENS_TZ = ZoneInfo("Europe/Athens")
TIMESTAMP = datetime(2026, 10, 22, 8, 0, tzinfo=ATHENS_TZ)
DAY_TYPE = "Winter Semester - Regular Teaching Day (Mon-Fri)"


@pytest.mark.asyncio
async def test_publishes_all_observations():
    exchange = AsyncMock()
    dataset = load_traffic_dataset()

    generator = SensorDataGenerator(
        dataset=dataset,
        interval_seconds=30,
        seed=42,
        day_type_override=DAY_TYPE,
    )

    count = await publish_observations(exchange, generator, TIMESTAMP)

    expected_lights = sum(
        len(intersection.light_ids)
        for intersection in dataset.intersections
    )
    expected_count = expected_lights * len(SENSOR_TYPES)

    assert count == expected_count
    assert exchange.publish.await_count == expected_count

    for call in exchange.publish.await_args_list:
        message = call.args[0]
        payload = json.loads(message.body)
        routing_key = call.kwargs["routing_key"]

        assert routing_key == (
            f"stls.sensor-data.input."
            f"{payload['intersection_id']}.{payload['sensor_type']}"
        )
        assert payload["light_id"]
        assert payload["count"] >= 0
        assert payload["schema_version"] == "1.0"
        assert message.content_type == "application/json"
        assert message.message_id == payload["event_id"]
        assert call.kwargs["mandatory"] is True
