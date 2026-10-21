from __future__ import annotations

import os
import random
from dataclasses import dataclass
from datetime import datetime
from uuid import UUID, uuid4
from zoneinfo import ZoneInfo

from dotenv import load_dotenv
from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator


GREECE_TZ = ZoneInfo("Europe/Athens")

DETECTION_TYPES = (
    "emergency-vehicle",
    "public-transport",
    "accident",
)

EXCHANGE_NAME = "stls.detection-events"
ROUTING_KEY_PREFIX = "stls.detection-events.input"


class DetectionEvent(BaseModel):
    """Synthetic detection event compatible with the STLS processor."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str = "1.0"
    event_id: UUID = Field(default_factory=uuid4)
    intersection_id: str = Field(min_length=1, max_length=100)
    light_id: str = Field(min_length=1, max_length=100)
    detection_type: str
    detected_at: datetime

    @field_validator("schema_version")
    @classmethod
    def validate_schema_version(cls, value: str) -> str:
        if value != "1.0":
            raise ValueError("schema_version must be '1.0'")
        return value

    @field_validator("detection_type")
    @classmethod
    def validate_detection_type(cls, value: str) -> str:
        if value not in DETECTION_TYPES:
            raise ValueError(f"Unsupported detection type: {value}")
        return value

    @field_validator("detected_at")
    @classmethod
    def require_timezone(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("detected_at must include a timezone")
        return value

    @model_validator(mode="after")
    def validate_identifiers(self) -> DetectionEvent:
        if not self.intersection_id.strip():
            raise ValueError("intersection_id must not be blank")
        if not self.light_id.strip():
            raise ValueError("light_id must not be blank")
        return self


@dataclass(frozen=True)
class IntersectionConfig:
    intersection_id: str
    light_ids: tuple[str, ...]


def load_intersections() -> tuple[IntersectionConfig, ...]:
    """Load configured intersection/light mappings."""
    load_dotenv()

    definitions = (
        ("STLS_INTERSECTION_EKKLISIA", "STLS_LIGHTS_EKKLISIA"),
        (
            "STLS_INTERSECTION_AGIOU_SPYRIDONOS",
            "STLS_LIGHTS_AGIOU_SPYRIDONOS",
        ),
        (
            "STLS_INTERSECTION_ANATOLIKI_PYLI",
            "STLS_LIGHTS_ANATOLIKI_PYLI",
        ),
    )

    intersections: list[IntersectionConfig] = []
    seen_ids: set[str] = set()

    for intersection_env, lights_env in definitions:
        intersection_id = os.getenv(intersection_env, "").strip()
        lights_value = os.getenv(lights_env, "").strip()

        if not intersection_id:
            raise RuntimeError(f"{intersection_env} must be configured")
        if not lights_value:
            raise RuntimeError(f"{lights_env} must be configured")

        light_ids = tuple(
            dict.fromkeys(
                item.strip()
                for item in lights_value.split(",")
                if item.strip()
            )
        )

        if not light_ids:
            raise RuntimeError(f"{lights_env} must contain light IDs")
        if intersection_id in seen_ids:
            raise RuntimeError(f"Duplicate intersection ID: {intersection_id}")

        seen_ids.add(intersection_id)
        intersections.append(
            IntersectionConfig(intersection_id, light_ids)
        )

    return tuple(intersections)


def build_routing_key(
    intersection_id: str,
    detection_type: str,
) -> str:
    """Build the RabbitMQ input routing key."""
    if not intersection_id or "." in intersection_id:
        raise ValueError("Invalid intersection ID for routing key")
    if detection_type not in DETECTION_TYPES:
        raise ValueError(f"Unsupported detection type: {detection_type}")

    return (
        f"{ROUTING_KEY_PREFIX}.{intersection_id}.{detection_type}"
    )


@dataclass(frozen=True)
class DetectionProfile:
    """Synthetic number of detection events generated per batch."""

    min_events: int
    max_events: int


DETECTION_PROFILES = {
    "quiet": DetectionProfile(1, 2),
    "normal": DetectionProfile(2, 4),
    "peak": DetectionProfile(5, 8),
    "overload": DetectionProfile(10, 15),
}


class DetectionEventGenerator:
    """Generate reproducible synthetic detection events."""

    def __init__(
        self,
        profile: str = "normal",
        seed: int | None = None,
    ) -> None:
        try:
            self.profile = DETECTION_PROFILES[profile]
        except KeyError as exc:
            raise ValueError(
                f"Unknown profile '{profile}'. Choose from: "
                f"{', '.join(DETECTION_PROFILES)}"
            ) from exc

        self.random = random.Random(seed)

    def generate_events(
        self,
        intersections: tuple[IntersectionConfig, ...],
        detected_at: datetime | None = None,
    ) -> list[DetectionEvent]:
        """Generate a batch of events across configured lights."""
        if not intersections:
            raise ValueError("At least one intersection must be configured")

        timestamp = detected_at or datetime.now(GREECE_TZ)

        if timestamp.tzinfo is None or timestamp.utcoffset() is None:
            raise ValueError("detected_at must include a timezone")

        event_count = self.random.randint(
            self.profile.min_events,
            self.profile.max_events,
        )

        events: list[DetectionEvent] = []

        for _ in range(event_count):
            intersection = self.random.choice(intersections)
            light_id = self.random.choice(intersection.light_ids)
            detection_type = self.random.choice(DETECTION_TYPES)

            events.append(
                DetectionEvent(
                    event_id=uuid4(),
                    intersection_id=intersection.intersection_id,
                    light_id=light_id,
                    detection_type=detection_type,
                    detected_at=timestamp,
                )
            )

        return events
