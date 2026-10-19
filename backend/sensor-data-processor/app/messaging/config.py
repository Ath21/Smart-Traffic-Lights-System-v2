
import os

def _required_env(name: str) -> str:
    value = os.getenv(name)
    if not value:
        raise RuntimeError(f"{name} must be configured in the environment")
    return value


def _parse_ids(name: str) -> set[str]:
    return {
        item.strip()
        for item in _required_env(name).split(",")
        if item.strip()
    }


EXCHANGE_NAME = _required_env("RABBITMQ_EXCHANGE_NAME")
EXCHANGE_TYPE = _required_env("RABBITMQ_EXCHANGE_TYPE")

INPUT_QUEUE = _required_env("RABBITMQ_INPUT_QUEUE")
INPUT_ROUTING_KEY = _required_env("RABBITMQ_INPUT_ROUTING_KEY")
OUTPUT_ROUTING_KEY_TEMPLATE = _required_env("RABBITMQ_OUTPUT_ROUTING_KEY")

RABBITMQ_URL = _required_env("RABBITMQ_URL")

INTERSECTION_LIGHTS = {
    _required_env("STLS_INTERSECTION_EKKLISIA"): _parse_ids(
        "STLS_LIGHTS_EKKLISIA"
    ),
    _required_env("STLS_INTERSECTION_AGIOU_SPYRIDONOS"): _parse_ids(
        "STLS_LIGHTS_AGIOU_SPYRIDONOS"
    ),
    _required_env("STLS_INTERSECTION_ANATOLIKI_PYLI"): _parse_ids(
        "STLS_LIGHTS_ANATOLIKI_PYLI"
    ),
}

if not RABBITMQ_URL:
    raise RuntimeError(
        "RABBITMQ_URL must be configured in the environment."
    )
