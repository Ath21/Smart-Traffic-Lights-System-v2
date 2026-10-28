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

from detection_event_generator import publish_detection_events
from detection_event_generator_common import (
    DETECTION_TYPES,
    EXCHANGE_NAME,
    DetectionEventGenerator,
    load_detection_dataset,
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
async def test_detection_event_generator_publishes_to_rabbitmq():
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

        # Use a temporary queue, not the backend processor queue.
        queue = await channel.declare_queue(
            exclusive=True,
            auto_delete=True,
        )
        await queue.bind(
            exchange,
            routing_key="stls.detection-events.input.#",
        )

        dataset = load_detection_dataset()
        generator = DetectionEventGenerator(
            dataset=dataset,
            interval_seconds=300,
            seed=42,
            load_factor=1000,
            day_type_override=DAY_TYPE,
        )
        detected_at = datetime(
            2026, 10, 22, 8, 0, tzinfo=ATHENS_TZ
        )

        # Generate once to determine the exact expected batch size.
        expected_events = generator.generate_events(detected_at)

        assert expected_events, (
            "No events generated; increase load_factor or inspect the dataset"
        )

        # Reset the seeded generator so the publisher generates the same batch.
        generator = DetectionEventGenerator(
            dataset=dataset,
            interval_seconds=300,
            seed=42,
            load_factor=1000,
            day_type_override=DAY_TYPE,
        )

        published_count = await publish_detection_events(
            exchange,
            generator,
            detected_at,
        )

        assert published_count == len(expected_events)

        received = []
        for _ in range(published_count):
            message = await asyncio.wait_for(
                queue.get(fail=False),
                timeout=10,
            )

            assert message is not None, (
                "Timed out waiting for a published detection event"
            )

            async with message.process():
                payload = json.loads(message.body.decode("utf-8"))

                assert message.content_type == "application/json"
                assert message.message_id == payload["event_id"]
                assert message.routing_key.startswith(
                    "stls.detection-events.input."
                )
                assert message.routing_key == (
                    "stls.detection-events.input."
                    f"{payload['intersection_id']}."
                    f"{payload['detection_type']}"
                )
                assert payload["schema_version"] == "1.0"
                assert payload["light_id"]
                assert payload["detection_type"] in DETECTION_TYPES
                assert payload["detected_at"]

                received.append(payload)

        assert len(received) == published_count
        assert len({item["event_id"] for item in received}) == published_count

    finally:
        await connection.close()
