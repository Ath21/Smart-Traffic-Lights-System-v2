
import asyncio
import json
import os
from uuid import uuid4

import aio_pika
from dotenv import load_dotenv


load_dotenv()

RABBITMQ_URL = os.getenv("RABBITMQ_URL")
EXCHANGE_NAME = os.getenv("RABBITMQ_EXCHANGE_NAME", "stls.sensor-data")
EXCHANGE_TYPE = os.getenv("RABBITMQ_EXCHANGE_TYPE", "topic")
OUTPUT_QUEUE = os.getenv("RABBITMQ_INTEGRATION_OUTPUT_QUEUE")

TEST_INTERSECTION_ID = os.getenv("RABBITMQ_TEST_INTERSECTION_ID")
TEST_LIGHT_ID = os.getenv("RABBITMQ_TEST_LIGHT_ID")
TEST_SENSOR_TYPE = os.getenv("RABBITMQ_TEST_SENSOR_TYPE")
TEST_COUNT = int(os.getenv("RABBITMQ_TEST_COUNT", "12"))


async def main() -> None:
    required_settings = {
        "RABBITMQ_URL": RABBITMQ_URL,
        "RABBITMQ_INTEGRATION_OUTPUT_QUEUE": OUTPUT_QUEUE,
        "RABBITMQ_TEST_INTERSECTION_ID": TEST_INTERSECTION_ID,
        "RABBITMQ_TEST_LIGHT_ID": TEST_LIGHT_ID,
        "RABBITMQ_TEST_SENSOR_TYPE": TEST_SENSOR_TYPE,
    }

    missing = [
        name for name, value in required_settings.items() if not value
    ]
    if missing:
        raise RuntimeError(
            f"Missing required .env settings: {', '.join(missing)}"
        )

    input_routing_key = (
        f"stls.sensor-data.input."
        f"{TEST_INTERSECTION_ID}.{TEST_SENSOR_TYPE}"
    )
    output_routing_key = (
        f"stls.sensor-data.processed."
        f"{TEST_INTERSECTION_ID}.{TEST_SENSOR_TYPE}"
    )

    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()

        exchange = await channel.declare_exchange(
            EXCHANGE_NAME,
            EXCHANGE_TYPE,
            durable=True,
        )

        queue = await channel.declare_queue(
            OUTPUT_QUEUE,
            durable=False,
            auto_delete=True,
        )
        await queue.bind(exchange, routing_key=output_routing_key)

        event_id = str(uuid4())

        payload = {
            "schema_version": "1.0",
            "event_id": event_id,
            "intersection_id": TEST_INTERSECTION_ID,
            "light_id": TEST_LIGHT_ID,
            "sensor_type": TEST_SENSOR_TYPE,
            "count": TEST_COUNT,
            "observed_at": "2026-10-19T12:00:00+03:00",
        }

        message = aio_pika.Message(
            body=json.dumps(payload).encode("utf-8"),
            content_type="application/json",
            message_id=event_id,
            delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
        )

        await exchange.publish(
            message,
            routing_key=input_routing_key,
            mandatory=True,
        )
        print(f"Published input event: {event_id}")

        try:
            async with asyncio.timeout(10):
                async with queue.iterator() as queue_iter:
                    incoming = await anext(queue_iter)

                    async with incoming.process():
                        result = json.loads(incoming.body)

                        assert result["event_id"] == event_id
                        assert (
                            result["intersection_id"]
                            == TEST_INTERSECTION_ID
                        )
                        assert result["light_id"] == TEST_LIGHT_ID
                        assert result["sensor_type"] == TEST_SENSOR_TYPE
                        assert result["count"] == TEST_COUNT
                        assert "processed_at" in result

                        print("PASS: Processed event received.")
                        print(f"Event ID: {result['event_id']}")
                        print(f"Intersection: {result['intersection_id']}")
                        print(f"Light: {result['light_id']}")
                        print(f"Count: {result['count']}")
                        print(f"Processed at: {result['processed_at']}")

        except TimeoutError:
            raise RuntimeError(
                "No processed event received within 10 seconds. "
                "Check the consumer, input queue binding, and output routing key."
            ) from None


if __name__ == "__main__":
    asyncio.run(main())
