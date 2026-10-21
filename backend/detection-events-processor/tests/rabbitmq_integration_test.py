import asyncio
import json
import os
from uuid import uuid4

import aio_pika
from dotenv import load_dotenv

load_dotenv()

RABBITMQ_URL = os.getenv("RABBITMQ_URL")
EXCHANGE_NAME = os.getenv(
    "RABBITMQ_EXCHANGE_NAME",
    "stls.detection-events",
)
OUTPUT_QUEUE = os.getenv("RABBITMQ_INTEGRATION_OUTPUT_QUEUE")

TEST_INTERSECTION_ID = os.getenv("RABBITMQ_TEST_INTERSECTION_ID")
TEST_LIGHT_ID = os.getenv("RABBITMQ_TEST_LIGHT_ID")
TEST_DETECTION_TYPE = os.getenv("RABBITMQ_TEST_DETECTION_TYPE", "accident")


async def main() -> None:
    required_settings = {
        "RABBITMQ_URL": RABBITMQ_URL,
        "RABBITMQ_INTEGRATION_OUTPUT_QUEUE": OUTPUT_QUEUE,
        "RABBITMQ_TEST_INTERSECTION_ID": TEST_INTERSECTION_ID,
        "RABBITMQ_TEST_LIGHT_ID": TEST_LIGHT_ID,
        "RABBITMQ_TEST_DETECTION_TYPE": TEST_DETECTION_TYPE,
    }

    missing = [
        name for name, value in required_settings.items() if not value
    ]
    if missing:
        raise RuntimeError(
            f"Missing required .env settings: {', '.join(missing)}"
        )

    input_routing_key = (
        f"stls.detection-events.input."
        f"{TEST_INTERSECTION_ID}.{TEST_DETECTION_TYPE}"
    )
    output_routing_key = (
        f"stls.detection-events.processed."
        f"{TEST_INTERSECTION_ID}.{TEST_DETECTION_TYPE}"
    )

    event_id = str(uuid4())

    payload = {
        "schema_version": "1.0",
        "event_id": event_id,
        "intersection_id": TEST_INTERSECTION_ID,
        "light_id": TEST_LIGHT_ID,
        "detection_type": TEST_DETECTION_TYPE,
        "detected_at": "2026-10-21T12:00:00+03:00",
    }

    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()

        exchange = await channel.declare_exchange(
            EXCHANGE_NAME,
            aio_pika.ExchangeType.TOPIC,
            durable=True,
        )

        output_queue = await channel.declare_queue(
            OUTPUT_QUEUE,
            durable=False,
            exclusive=True,
            auto_delete=True,
        )
        await output_queue.bind(
            exchange,
            routing_key=output_routing_key,
        )

        message = aio_pika.Message(
            body=json.dumps(payload).encode("utf-8"),
            content_type="application/json",
            message_id=event_id,
            type=TEST_DETECTION_TYPE,
            delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
        )

        await exchange.publish(
            message,
            routing_key=input_routing_key,
            mandatory=True,
        )
        print(f"Published input event: {event_id}")
        print(f"Input routing key: {input_routing_key}")
        print(f"Waiting for output routing key: {output_routing_key}")

        try:
            async with asyncio.timeout(15):
                async with output_queue.iterator() as queue_iterator:
                    incoming = await anext(queue_iterator)

                    async with incoming.process():
                        result = json.loads(incoming.body)

                        assert result["event_id"] == event_id
                        assert (
                            result["intersection_id"]
                            == TEST_INTERSECTION_ID
                        )
                        assert result["light_id"] == TEST_LIGHT_ID
                        assert (
                            result["detection_type"]
                            == TEST_DETECTION_TYPE
                        )
                        assert "processed_at" in result

                        print("PASS: Processed detection event received.")
                        print(f"Event ID: {result['event_id']}")
                        print(f"Intersection: {result['intersection_id']}")
                        print(f"Light: {result['light_id']}")
                        print(f"Detection type: {result['detection_type']}")
                        print(f"Processed at: {result['processed_at']}")

        except TimeoutError:
            raise RuntimeError(
                "No processed detection event received within 15 seconds. "
                "Check processor readiness, RabbitMQ bindings, and logs."
            ) from None


if __name__ == "__main__":
    asyncio.run(main())
