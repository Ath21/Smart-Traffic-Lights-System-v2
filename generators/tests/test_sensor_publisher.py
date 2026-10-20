
import json
from unittest.mock import AsyncMock

import pytest

from sensor_data_generator_common import (
    IntersectionConfig,
    SensorDataGenerator,
)
from sensor_data_generator import publish_observations


@pytest.mark.asyncio
async def test_publishes_all_observations() -> None:
    exchange = AsyncMock()
    intersections = (
        IntersectionConfig(
            intersection_id="ekklisia-3",
            light_ids=("dimitsanas-301S",),
        ),
    )
    generator = SensorDataGenerator(profile="normal", seed=42)

    count = await publish_observations(
        exchange,
        generator,
        intersections,
    )

    assert count == 3
    assert exchange.publish.await_count == 3

    calls = exchange.publish.await_args_list
    routing_keys = {call.kwargs["routing_key"] for call in calls}

    assert routing_keys == {
        "stls.sensor-data.input.ekklisia-3.vehicle-count",
        "stls.sensor-data.input.ekklisia-3.pedestrian-count",
        "stls.sensor-data.input.ekklisia-3.bicycle-count",
    }

    for call in calls:
        message = call.args[0]
        payload = json.loads(message.body)

        assert payload["intersection_id"] == "ekklisia-3"
        assert payload["light_id"] == "dimitsanas-301S"
        assert payload["count"] >= 0
        assert call.kwargs["mandatory"] is True

