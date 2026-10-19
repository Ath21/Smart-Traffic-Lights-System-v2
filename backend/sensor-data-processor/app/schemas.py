from datetime import datetime, timezone
from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator

IntersectionId = Literal[
        "anatoliki-pyli-1",
        "agiou-spyridonos-2",
        "ekklisia-3",
]

SensorType = Literal[
        "vehicle-count",
        "pedestrian-count",
        "bicycle-count",
]

class SensorObservation(BaseModel):
    """Raw sensor observation received from a workload generator."""

    model_config = ConfigDict(extra="forbid")

    schema_version: Literal["1.0"] = "1.0"
    event_id: UUID
    intersection_id: IntersectionId
    sensor_type: SensorType
    sensor_id: str = Field(min_length=1, max_length=100)
    count: int = Field(ge=0)
    observed_at: datetime

    @field_validator("observed_at")
    @classmethod
    def require_timezone(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("observed_at must include a timezone")
        return value

class ProcessedSensorObservation(SensorObservation):
    """Validated sensor observation enriched by the processor."""

    processed_at: datetime = Field(
            default_factory=lambda: datetime.now(timezone.utc)
    )

    @field_validator("processed_at")
    @classmethod
    def require_processed_timezone(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("processed_at must include a timezone")
        return value
