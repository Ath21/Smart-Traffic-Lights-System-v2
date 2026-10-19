
import os

EXCHANGE_NAME = "stls.events"
EXCHANGE_TYPE = "topic"

INPUT_QUEUE = "sensor-data-processor.input"

INPUT_ROUTING_KEY = "stls.sensor-data.input.*.*"

OUTPUT_ROUTING_KEY_TEMPLATE = (
    "stls.sensor-data.processed.{intersection}.{sensor_type}"
)

RABBITMQ_URL = os.getenv(
    "RABBITMQ_URL",
    "amqp://guest:guest@localhost:5672/",
)

