
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

SENSOR_TYPES = (
    "vehicle-count",
    "pedestrian-count",
    "bicycle-count",
)

EXCHANGE_NAME = "stls.sensor-data"
ROUTING_KEY_PREFIX = "stls.sensor-data.input"


class SensorObservation(BaseModel):
    """Raw synthetic observation compatible with the STLS processor."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str = "1.0"
    event_id: UUID = Field(default_factory=uuid4)
    intersection_id: str = Field(min_length=1, max_length=100)
    light_id: str = Field(min_length=1, max_length=100)
    sensor_type: str
    count: int = Field(ge=0)
    observed_at: datetime

    @field_validator("schema_version")
    @classmethod
    def validate_schema_version(cls, value: str) -> str:
        if value != "1.0":
            raise ValueError("schema_version must be '1.0'")
        return value

    @field_validator("sensor_type")
    @classmethod
    def validate_sensor_type(cls, value: str) -> str:
        if value not in SENSOR_TYPES:
            raise ValueError(f"Unsupported sensor type: {value}")
        return value

    @field_validator("observed_at")
    @classmethod
    def require_timezone(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("observed_at must include a timezone")
        return value

    @model_validator(mode="after")
    def validate_identifiers(self) -> SensorObservation:
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
    """Load the intersection/light mapping from environment variables."""
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
    seen_intersections: set[str] = set()

    for intersection_env, lights_env in definitions:
        intersection_id = os.getenv(intersection_env, "").strip()
        lights_value = os.getenv(lights_env, "").strip()

        if not intersection_id:
            raise RuntimeError(f"{intersection_env} must be configured")
        if not lights_value:
            raise RuntimeError(f"{lights_env} must be configured")

        light_ids = tuple(
            dict.fromkeys(
                light.strip()
                for light in lights_value.split(",")
                if light.strip()
            )
        )

        if not light_ids:
            raise RuntimeError(f"{lights_env} must contain light IDs")
        if intersection_id in seen_intersections:
            raise RuntimeError(
                f"Duplicate intersection ID: {intersection_id}"
            )

        seen_intersections.add(intersection_id)
        intersections.append(
            IntersectionConfig(
                intersection_id=intersection_id,
                light_ids=light_ids,
            )
        )

    return tuple(intersections)


def build_routing_key(
    intersection_id: str,
    sensor_type: str,
) -> str:
    """Build the input routing key for one observation."""
    if not intersection_id or "." in intersection_id:
        raise ValueError("Invalid intersection ID for routing key")
    if sensor_type not in SENSOR_TYPES:
        raise ValueError(f"Unsupported sensor type: {sensor_type}")

    return f"{ROUTING_KEY_PREFIX}.{intersection_id}.{sensor_type}"


@dataclass(frozen=True)
class TrafficProfile:
    """Illustrative synthetic mean counts per observation interval."""

    vehicle_mean: float
    pedestrian_mean: float
    bicycle_mean: float


TRAFFIC_PROFILES = {
    "quiet": TrafficProfile(2.0, 0.5, 0.3),
    "normal": TrafficProfile(6.0, 2.0, 1.0),
    "peak": TrafficProfile(14.0, 5.0, 2.5),
    "overload": TrafficProfile(25.0, 8.0, 4.0),
}


class SensorDataGenerator:
    """Create reproducible synthetic observations."""

    def __init__(
        self,
        profile: str = "normal",
        seed: int | None = None,
    ) -> None:
        try:
            self.profile = TRAFFIC_PROFILES[profile]
        except KeyError as exc:
            raise ValueError(
                f"Unknown profile '{profile}'. "
                f"Choose from: {', '.join(TRAFFIC_PROFILES)}"
            ) from exc

        self.random = random.Random(seed)

    def generate_observations(
        self,
        intersections: tuple[IntersectionConfig, ...],
        observed_at: datetime | None = None,
    ) -> list[SensorObservation]:
        """Generate one observation per light and sensor type."""
        timestamp = observed_at or datetime.now(GREECE_TZ)

        if timestamp.tzinfo is None or timestamp.utcoffset() is None:
            raise ValueError("observed_at must include a timezone")

        observations: list[SensorObservation] = []

        means = {
            "vehicle-count": self.profile.vehicle_mean,
            "pedestrian-count": self.profile.pedestrian_mean,
            "bicycle-count": self.profile.bicycle_mean,
        }

        for intersection in intersections:
            for light_id in intersection.light_ids:
                for sensor_type in SENSOR_TYPES:
                    count = self.random.poisson(means[sensor_type]) if hasattr(
                        self.random, "poisson"
                    ) else _poisson_sample(
                        self.random, means[sensor_type]
                    )

                    observations.append(
                        SensorObservation(
                            intersection_id=intersection.intersection_id,
                            light_id=light_id,
                            sensor_type=sensor_type,
                            count=count,
                            observed_at=timestamp,
                        )
                    )

        return observations


def _poisson_sample(
    rng: random.Random,
    mean: float,
) -> int:
    """Sample a Poisson count without third-party numerical libraries."""
    # Knuth's algorithm is suitable for the small means used here.
    import math

    threshold = math.exp(-mean)
    product = 1.0
    count = 0

    while product > threshold:
        count += 1
        product *= rng.random()

    return count - 1

