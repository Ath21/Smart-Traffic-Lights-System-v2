from __future__ import annotations

import os
import random
from datetime import datetime
from pathlib import Path
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

from stls_dataset_common import (
    GREECE_TZ,
    IntersectionConfig,
    LoadedDataset,
    load_dataset,
    poisson_sample,
)


SENSOR_TYPES = (
    "vehicle-count",
    "pedestrian-count",
    "bicycle-count",
)

EXCHANGE_NAME = "stls.sensor-data"
ROUTING_KEY_PREFIX = "stls.sensor-data.input"

DEFAULT_DATASET_PATH = Path(__file__).with_name("../../dataset/stls_traffic_counts.json")

# Dataset field holding the hourly {"min", "max"} range for each sensor type.
DATASET_FIELDS = {
    "vehicle-count": "vehicles_per_hour",
    "pedestrian-count": "pedestrians_per_hour",
    "bicycle-count": "cyclists_per_hour",
}


class SensorObservation(BaseModel):
    """Synthetic observation compatible with the STLS processor."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str = "1.0"
    event_id: UUID
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


def load_traffic_dataset(path: str | Path | None = None) -> LoadedDataset:
    """Load the traffic-counts dataset (path arg > env var > default)."""
    resolved = (
        path
        or os.getenv("STLS_TRAFFIC_COUNTS_PATH", "").strip()
        or DEFAULT_DATASET_PATH
    )
    return load_dataset(resolved, "counts_data")


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


class SensorDataGenerator:
    """Create synthetic observations driven by the traffic-counts dataset.

    The dataset gives an hourly [min, max] range per light, day type and
    hour of day. For each observation a rate is drawn uniformly from that
    range, converted to the expected count over the observation window,
    and sampled from a Poisson distribution.
    """

    def __init__(
        self,
        dataset: LoadedDataset,
        interval_seconds: float,
        seed: int | None = None,
        load_factor: float = 1.0,
        day_type_override: str | None = None,
    ) -> None:
        if interval_seconds <= 0:
            raise ValueError("interval_seconds must be greater than zero")
        if load_factor < 0:
            raise ValueError("load_factor must not be negative")

        self.dataset = dataset
        self.interval_seconds = interval_seconds
        self.load_factor = load_factor
        self.day_type_override = day_type_override
        self.random = random.Random(seed)

    @property
    def intersections(self) -> tuple[IntersectionConfig, ...]:
        return self.dataset.intersections

    def day_type_at(self, timestamp: datetime) -> str:
        return self.day_type_override or self.dataset.calendar.day_type_for(
            timestamp.date()
        )

    def generate_observations(
        self,
        observed_at: datetime | None = None,
    ) -> list[SensorObservation]:
        """Generate one observation per light and sensor type."""
        timestamp = observed_at or datetime.now(GREECE_TZ)

        if timestamp.tzinfo is None or timestamp.utcoffset() is None:
            raise ValueError("observed_at must include a timezone")

        local = timestamp.astimezone(GREECE_TZ)
        day_type = self.day_type_at(local)
        hour = local.hour
        window_fraction = self.interval_seconds / 3600.0

        observations: list[SensorObservation] = []

        for intersection in self.intersections:
            for light_id in intersection.light_ids:
                record = self.dataset.record(light_id, day_type, hour)

                for sensor_type in SENSOR_TYPES:
                    bounds = record[DATASET_FIELDS[sensor_type]]
                    hourly_rate = self.random.uniform(
                        bounds["min"], bounds["max"]
                    )
                    mean = hourly_rate * self.load_factor * window_fraction
                    count = poisson_sample(self.random, mean)

                    observations.append(
                        SensorObservation(
                            # Seeded, so runs stay reproducible.
                            event_id=UUID(
                                int=self.random.getrandbits(128),
                                version=4,
                            ),
                            intersection_id=intersection.intersection_id,
                            light_id=light_id,
                            sensor_type=sensor_type,
                            count=count,
                            observed_at=timestamp,
                        )
                    )

        return observations
