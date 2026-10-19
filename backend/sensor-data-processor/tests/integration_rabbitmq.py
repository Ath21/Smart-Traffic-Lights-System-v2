import asyncio
import json
import os
from uuid import uuid4

import aio_pika


RABBITMQ_URL = os.getenv(
    "RABBITMQ_URL",
    "amqp://stls_dev:stls_dev_password@localhost:5672/",
)

EXCHANGE_NAME = "stls.events"
OUTPUT_QUEUE = "sensor-data-processor.integration-test"
OUTPUT_ROUTING_KEY = (
    "stls.sensor-data.processed.anatoliki-pyli-1.vehicle-count"
)


async def main() -> None:
    connection = await aio_pika.connect_robust(RABBITMQ_URL)

    async with connection:
        channel = await connection.channel()
        exchange = await channel.declare_exchange(
            EXCHANGE_NAME,
            aio_pika.ExchangeType.TOPIC,
            durable=True,
        )

        queue = await channel.declare_queue(
            OUTPUT_QUEUE,
            durable=True,
            auto_delete=True,
        )
        await queue.bind(exchange, routing_key=OUTPUT_ROUTING_KEY)

        event_id = str(uuid4())
        payload = {
            "schema_version": "1.0",
            "event_id": event_id,
            "intersection_id": "anatoliki-pyli-1",
            "sensor_type": "vehicle-count",
            "sensor_id": "test-sensor-001",
            "count": 12,
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
            routing_key=(
                "stls.sensor-data.input."
                "anatoliki-pyli-1.vehicle-count"
            ),
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
                            == "anatoliki-pyli-1"
                        )
                        assert result["sensor_type"] == "vehicle-count"
                        assert result["count"] == 12
                        assert "processed_at" in result

                        print("PASS: Processed event received.")
                        print(f"Event ID: {result['event_id']}")
                        print(f"Count: {result['count']}")
                        print(f"Processed at: {result['processed_at']}")

        except TimeoutError:
            raise RuntimeError(
                "No processed event received within 10 seconds. "
                "Check the consumer terminal and RabbitMQ queue bindings."
            ) from None



if __name__ == "__main__":
    asyncio.run(main())
