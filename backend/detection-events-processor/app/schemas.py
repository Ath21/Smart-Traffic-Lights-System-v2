from datetime import datetime
from uuid import UUID
from zoneinfo import ZoneInfo

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

from app.config import INTERSECTION_LIGHTS

GREECE_TZ = ZoneInfo("Europe/Athens")

DETECTION_TYPES = (
    "emergency-vehicle",
    "public-transport",
    "accident",
)


class DetectionEvent(BaseModel):
    """A single detected event associated with a configured traffic light."""

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
            raise ValueError(
                f"detection_type must be one of {list(DETECTION_TYPES)}"
            )
        return value

    @field_validator("detected_at")
    @classmethod
    def require_timezone(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("detected_at must include a timezone")
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


class ProcessedDetectionEvent(DetectionEvent):
    """Validated event enriched with the time processing occurred."""

    processed_at: datetime = Field(
        default_factory=lambda: datetime.now(GREECE_TZ)
    )

    @field_validator("processed_at")
    @classmethod
    def require_processed_timezone(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("processed_at must include a timezone")
        return value
