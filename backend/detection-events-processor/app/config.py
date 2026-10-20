import os
from pathlib import Path

from dotenv import load_dotenv

# Load this service's local environment file in development.
# Variables already exported by the environment take precedence.
load_dotenv(Path(__file__).resolve().parents[1] / ".env")


def _required_env(name: str) -> str:
    value = os.getenv(name, "").strip()
    if not value:
        raise RuntimeError(f"{name} must be configured")
    return value


def _positive_int(name: str) -> int:
    raw_value = _required_env(name)
    try:
        value = int(raw_value)
    except ValueError as exc:
        raise RuntimeError(f"{name} must be an integer") from exc

    if value <= 0:
        raise RuntimeError(f"{name} must be greater than zero")
    return value


def _parse_ids(name: str) -> set[str]:
    value = _required_env(name)
    identifiers = {
        item.strip()
        for item in value.split(",")
        if item.strip()
    }
    if not identifiers:
        raise RuntimeError(f"{name} must contain at least one ID")
    return identifiers


RABBITMQ_URL = _required_env("RABBITMQ_URL")
RABBITMQ_EXCHANGE_NAME = _required_env("RABBITMQ_EXCHANGE_NAME")
RABBITMQ_INPUT_QUEUE = _required_env("RABBITMQ_INPUT_QUEUE")
RABBITMQ_INPUT_ROUTING_KEY = _required_env("RABBITMQ_INPUT_ROUTING_KEY")
RABBITMQ_OUTPUT_ROUTING_KEY_TEMPLATE = _required_env(
    "RABBITMQ_OUTPUT_ROUTING_KEY_TEMPLATE"
)
REDIS_URL = _required_env("REDIS_URL")

DETECTION_EVENT_CACHE_TTL_SECONDS = _positive_int(
    "DETECTION_EVENT_CACHE_TTL_SECONDS"
)
EVENT_DEDUP_TTL_SECONDS = _positive_int("EVENT_DEDUP_TTL_SECONDS")

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
