
from datetime import datetime
from uuid import UUID
from zoneinfo import ZoneInfo

from pydantic import (
    BaseModel,
    ConfigDict,
    Field,
    field_validator,
    model_validator,
)

from app.config import INTERSECTION_LIGHTS


GREECE_TZ = ZoneInfo("Europe/Athens")

TrafficType = str


class TrafficCount(BaseModel):
    """Raw traffic observation generated for a specific traffic light."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str = "1.0"
    event_id: UUID
    intersection_id: str = Field(min_length=1, max_length=100)
    light_id: str = Field(min_length=1, max_length=100)
    traffic_type: TrafficType
    count: int = Field(ge=0)
    observed_at: datetime

    @field_validator("schema_version")
    @classmethod
    def validate_schema_version(cls, value: str) -> str:
        if value != "1.0":
            raise ValueError("schema_version must be '1.0'")
        return value

    @field_validator("traffic_type")
    @classmethod
    def validate_traffic_type(cls, value: str) -> str:
        allowed = {
            "vehicle-count",
            "pedestrian-count",
            "bicycle-count",
        }
        if value not in allowed:
            raise ValueError(
                f"traffic_type must be one of {sorted(allowed)}"
            )
        return value

    @field_validator("observed_at")
    @classmethod
    def require_timezone(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("observed_at must include a timezone")
        return value

    @model_validator(mode="after")
    def validate_light_assignment(self):
        allowed_lights = INTERSECTION_LIGHTS.get(self.intersection_id)

        if allowed_lights is None:
            raise ValueError(
                f"Unknown intersection_id: {self.intersection_id}"
            )

        if self.light_id not in allowed_lights:
            raise ValueError(
                f"Light '{self.light_id}' does not belong to "
                f"intersection '{self.intersection_id}'"
            )

        return self


class ProcessedTrafficCount(TrafficCount):
    """Validated traffic count observation enriched by the processor."""

    processed_at: datetime = Field(
        default_factory=lambda: datetime.now(GREECE_TZ)
    )

    @field_validator("processed_at")
    @classmethod
    def require_processed_timezone(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("processed_at must include a timezone")
        return value
