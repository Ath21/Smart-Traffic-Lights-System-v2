from __future__ import annotations

import os
import random
from datetime import datetime
from pathlib import Path
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

from generators.dataset import (
    GREECE_TZ,
    IntersectionConfig,
    LoadedDataset,
    load_dataset,
    probability_over_windows,
)


DETECTION_TYPES = (
    "emergency-vehicle",
    "public-transport",
    "accident",
)

EXCHANGE_NAME = "stls.detection-events"
ROUTING_KEY_PREFIX = "stls.detection-events.input"

DEFAULT_DATASET_PATH = Path(__file__).with_name("../../dataset/stls_detection_events.json")

BUS_WINDOW_SECONDS = 300.0  # dataset gives bus probability per 5 minutes
DEFAULT_SIGNAL_CYCLE_SECONDS = 90.0


class DetectionEvent(BaseModel):
    """Synthetic detection event compatible with the STLS processor."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str = "1.0"
    event_id: UUID
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


def load_detection_dataset(path: str | Path | None = None) -> LoadedDataset:
    """Load the detection-events dataset (path arg > env var > default)."""
    resolved = (
        path
        or os.getenv("STLS_DETECTION_EVENTS_PATH", "").strip()
        or DEFAULT_DATASET_PATH
    )
    return load_dataset(resolved, "events_data")


def build_routing_key(
    intersection_id: str,
    detection_type: str,
) -> str:
    """Build the RabbitMQ input routing key."""
    if not intersection_id or "." in intersection_id:
        raise ValueError("Invalid intersection ID for routing key")
    if detection_type not in DETECTION_TYPES:
        raise ValueError(f"Unsupported detection type: {detection_type}")

    return f"{ROUTING_KEY_PREFIX}.{intersection_id}.{detection_type}"


class DetectionEventGenerator:
    """Generate synthetic detection events driven by the events dataset.

    For every light, each batch makes one independent Bernoulli draw per
    detection type. The dataset probabilities (per 5 minutes for buses,
    per signal cycle for emergency vehicles and incidents) are converted
    to the probability of at least one event within the batch window.
    """

    def __init__(
        self,
        dataset: LoadedDataset,
        interval_seconds: float,
        seed: int | None = None,
        load_factor: float = 1.0,
        cycle_seconds: float = DEFAULT_SIGNAL_CYCLE_SECONDS,
        day_type_override: str | None = None,
    ) -> None:
        if interval_seconds <= 0:
            raise ValueError("interval_seconds must be greater than zero")
        if cycle_seconds <= 0:
            raise ValueError("cycle_seconds must be greater than zero")
        if load_factor < 0:
            raise ValueError("load_factor must not be negative")

        self.dataset = dataset
        self.interval_seconds = interval_seconds
        self.cycle_seconds = cycle_seconds
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

    def generate_events(
        self,
        detected_at: datetime | None = None,
    ) -> list[DetectionEvent]:
        """Generate the events that occur across all lights in one batch."""
        timestamp = detected_at or datetime.now(GREECE_TZ)

        if timestamp.tzinfo is None or timestamp.utcoffset() is None:
            raise ValueError("detected_at must include a timezone")

        local = timestamp.astimezone(GREECE_TZ)
        day_type = self.day_type_at(local)
        hour = local.hour

        bus_windows = self.interval_seconds / BUS_WINDOW_SECONDS
        cycles = self.interval_seconds / self.cycle_seconds

        events: list[DetectionEvent] = []

        for intersection in self.intersections:
            for light_id in intersection.light_ids:
                record = self.dataset.record(light_id, day_type, hour)

                bus = record["public_transport_bus_probability_per_5min"]
                bus_p = self.random.uniform(bus["min"], bus["max"])

                probabilities = {
                    "public-transport": probability_over_windows(
                        bus_p * self.load_factor, bus_windows
                    ),
                    "emergency-vehicle": probability_over_windows(
                        record["emergency_vehicle_probability_per_signal_cycle"]
                        * self.load_factor,
                        cycles,
                    ),
                    "accident": probability_over_windows(
                        record["traffic_incident_probability_per_signal_cycle"]
                        * self.load_factor,
                        cycles,
                    ),
                }

                # Draw for every type every time so the RNG stream stays
                # aligned regardless of which events fire.
                for detection_type in DETECTION_TYPES:
                    draw = self.random.random()
                    event_id = UUID(
                        int=self.random.getrandbits(128), version=4
                    )

                    if draw < probabilities[detection_type]:
                        events.append(
                            DetectionEvent(
                                event_id=event_id,
                                intersection_id=intersection.intersection_id,
                                light_id=light_id,
                                detection_type=detection_type,
                                detected_at=timestamp,
                            )
                        )

        return events
