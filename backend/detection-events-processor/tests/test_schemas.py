from uuid import uuid4
from zoneinfo import ZoneInfo

import pytest
from pydantic import ValidationError

from app.config import INTERSECTION_LIGHTS
from app.schemas import DetectionEvent, ProcessedDetectionEvent

TEST_INTERSECTION_ID = next(iter(INTERSECTION_LIGHTS))
TEST_LIGHT_ID = next(iter(INTERSECTION_LIGHTS[TEST_INTERSECTION_ID]))


def valid_payload() -> dict:
    return {
        "schema_version": "1.0",
        "event_id": str(uuid4()),
        "intersection_id": TEST_INTERSECTION_ID,
        "light_id": TEST_LIGHT_ID,
        "detection_type": "emergency-vehicle",
        "detected_at": "2026-10-21T10:15:30Z",
    }


def test_valid_detection_event():
    event = DetectionEvent.model_validate(valid_payload())

    assert event.intersection_id == TEST_INTERSECTION_ID
    assert event.light_id == TEST_LIGHT_ID
    assert event.detection_type == "emergency-vehicle"


@pytest.mark.parametrize(
    "detection_type",
    ["emergency-vehicle", "public-transport", "accident"],
)
def test_supported_detection_types(detection_type):
    payload = valid_payload()
    payload["detection_type"] = detection_type

    event = DetectionEvent.model_validate(payload)

    assert event.detection_type == detection_type


def test_unknown_detection_type_is_rejected():
    payload = valid_payload()
    payload["detection_type"] = "motorcycle-count"

    with pytest.raises(ValidationError):
        DetectionEvent.model_validate(payload)


def test_unknown_intersection_is_rejected():
    payload = valid_payload()
    payload["intersection_id"] = "unknown-intersection"

    with pytest.raises(ValidationError):
        DetectionEvent.model_validate(payload)


def test_light_from_another_intersection_is_rejected():
    intersections = list(INTERSECTION_LIGHTS)

    if len(intersections) < 2:
        pytest.skip("At least two configured intersections are required")

    first, second = intersections[:2]
    other_light = next(iter(INTERSECTION_LIGHTS[second]))

    payload = valid_payload()
    payload["intersection_id"] = first
    payload["light_id"] = other_light

    if other_light in INTERSECTION_LIGHTS[first]:
        pytest.skip("Selected light belongs to both intersections")

    with pytest.raises(ValidationError):
        DetectionEvent.model_validate(payload)


def test_timezone_is_required():
    payload = valid_payload()
    payload["detected_at"] = "2026-10-21T10:15:30"

    with pytest.raises(ValidationError):
        DetectionEvent.model_validate(payload)


def test_unexpected_fields_are_rejected():
    payload = valid_payload()
    payload["count"] = 12

    with pytest.raises(ValidationError):
        DetectionEvent.model_validate(payload)


def test_sensor_id_is_not_accepted():
    payload = valid_payload()
    payload["sensor_id"] = "legacy-sensor-id"

    with pytest.raises(ValidationError):
        DetectionEvent.model_validate(payload)


def test_invalid_schema_version_is_rejected():
    payload = valid_payload()
    payload["schema_version"] = "2.0"

    with pytest.raises(ValidationError):
        DetectionEvent.model_validate(payload)


def test_processed_event_adds_timestamp():
    event = ProcessedDetectionEvent.model_validate(valid_payload())

    assert event.processed_at.tzinfo is not None
    assert event.processed_at.tzinfo == ZoneInfo("Europe/Athens")
