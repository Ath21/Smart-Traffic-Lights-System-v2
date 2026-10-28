from __future__ import annotations

import asyncio
import json
import os
from datetime import datetime
from pathlib import Path
from zoneinfo import ZoneInfo

import aio_pika
import pytest
from dotenv import load_dotenv

from traffic_count_generator import publish_observations
from traffic_count_generator_common import (
    EXCHANGE_NAME,
    SENSOR_TYPES,
    SensorDataGenerator,
    load_traffic_dataset,
)

load_dotenv(
    Path(__file__).resolve().parents[2] / ".env",
    override=False,
)

pytestmark = [
    pytest.mark.integration,
    pytest.mark.skipif(
        os.getenv("RUN_RABBITMQ_INTEGRATION") != "1",
        reason="Set RUN_RABBITMQ_INTEGRATION=1 to run broker integration tests",
    ),
]

ATHENS_TZ = ZoneInfo("Europe/Athens")
DAY_TYPE = "Winter Semester - Regular Teaching Day (Mon-Fri)"


@pytest.mark.asyncio
async def test_traffic_count_generator_publishes_to_rabbitmq():
    rabbitmq_url = os.getenv("RABBITMQ_URL", "").strip()
    exchange_name = os.getenv(
        "RABBITMQ_EXCHANGE_NAME",
        EXCHANGE_NAME,
    ).strip()

    if not rabbitmq_url:
        pytest.fail("RABBITMQ_URL is missing from the generator .env")
    if exchange_name != EXCHANGE_NAME:
        pytest.fail(
            f"Expected RABBITMQ_EXCHANGE_NAME={EXCHANGE_NAME!r}, "
            f"got {exchange_name!r}"
        )

    connection = await aio_pika.connect_robust(rabbitmq_url)

    try:
        channel = await connection.channel(publisher_confirms=True)

        exchange = await channel.declare_exchange(
            exchange_name,
            aio_pika.ExchangeType.TOPIC,
            durable=True,
        )

        # Temporary queue: this test does not consume from the processor queue.
        queue = await channel.declare_queue(
            exclusive=True,
            auto_delete=True,
        )

        await queue.bind(
            exchange,
            routing_key="stls.traffic-count.input.#",
        )

        dataset = load_traffic_dataset()
        generator = SensorDataGenerator(
            dataset=dataset,
            interval_seconds=30,
            seed=42,
            day_type_override=DAY_TYPE,
        )
        observed_at = datetime(
            2026, 10, 22, 8, 0, tzinfo=ATHENS_TZ
        )

        expected_count = sum(
            len(intersection.light_ids)
            for intersection in dataset.intersections
        ) * len(SENSOR_TYPES)

        published_count = await publish_observations(
            exchange,
            generator,
            observed_at,
        )

        assert published_count == expected_count

        received = []
        for _ in range(expected_count):
            message = await asyncio.wait_for(
                queue.get(fail=False),
                timeout=10,
            )

            assert message is not None, (
                "Timed out waiting for a published traffic-count message"
            )

            async with message.process():
                payload = json.loads(message.body.decode("utf-8"))

                assert message.content_type == "application/json"
                assert message.message_id == payload["event_id"]
                assert message.routing_key.startswith(
                    "stls.traffic-count.input."
                )
                assert message.routing_key == (
                    "stls.traffic-count.input."
                    f"{payload['intersection_id']}."
                    f"{payload['sensor_type']}"
                )
                assert payload["schema_version"] == "1.0"
                assert payload["light_id"]
                assert payload["sensor_type"] in SENSOR_TYPES
                assert isinstance(payload["count"], int)
                assert payload["count"] >= 0
                assert payload["observed_at"]

                received.append(payload)

        assert len(received) == expected_count
        assert len({item["event_id"] for item in received}) == expected_count

    finally:
        await connection.close()
