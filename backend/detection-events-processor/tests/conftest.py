import os

# Keep schema unit tests independent of a running RabbitMQ or Redis instance.
# These are test-only values; real service configuration comes from .env.
TEST_ENV = {
    "RABBITMQ_URL": "amqp://guest:guest@localhost:5672/",
    "RABBITMQ_EXCHANGE_NAME": "stls.detection-events",
    "RABBITMQ_INPUT_QUEUE": "stls.detection-events.processor",
    "RABBITMQ_INPUT_ROUTING_KEY": "stls.detection-events.input.#",
    "RABBITMQ_OUTPUT_ROUTING_KEY_TEMPLATE": (
        "stls.detection-events.processed.{intersection}.{detection_type}"
    ),
    "REDIS_URL": "redis://localhost:6379/0",
    "DETECTION_EVENT_CACHE_TTL_SECONDS": "300",
    "EVENT_DEDUP_TTL_SECONDS": "86400",
    "STLS_INTERSECTION_EKKLISIA": "ekklisia-3",
    "STLS_LIGHTS_EKKLISIA": (
        "dimitsanas-301S,edessis-302W,korytsas-303E"
    ),
    "STLS_INTERSECTION_AGIOU_SPYRIDONOS": "agiou-spyridonos-2",
    "STLS_LIGHTS_AGIOU_SPYRIDONOS": (
        "agiou-spyridon201W,dimitsanas-202N"
    ),
    "STLS_INTERSECTION_ANATOLIKI_PYLI": "anatoliki-pyli-1",
    "STLS_LIGHTS_ANATOLIKI_PYLI": "mhlou-101N,agiou-spyridon-102W",
}

for name, value in TEST_ENV.items():
    os.environ.setdefault(name, value)
