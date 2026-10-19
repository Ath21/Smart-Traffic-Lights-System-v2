
from datetime import timezone
from uuid import uuid4

import pytest
from pydantic import ValidationError

from app.config import INTERSECTION_LIGHTS
from app.schemas import ProcessedSensorObservation, SensorObservation


TEST_INTERSECTION_ID = next(iter(INTERSECTION_LIGHTS))
TEST_LIGHT_ID = next(iter(INTERSECTION_LIGHTS[TEST_INTERSECTION_ID]))


def valid_payload() -> dict:
    return {
        "schema_version": "1.0",
        "event_id": str(uuid4()),
        "intersection_id": TEST_INTERSECTION_ID,
        "light_id": TEST_LIGHT_ID,
        "sensor_type": "vehicle-count",
        "count": 12,
        "observed_at": "2026-10-19T10:15:30Z",
    }


def test_valid_sensor_observation():
    observation = SensorObservation.model_validate(valid_payload())

    assert observation.intersection_id == TEST_INTERSECTION_ID
    assert observation.light_id == TEST_LIGHT_ID
    assert observation.sensor_type == "vehicle-count"
    assert observation.count == 12


def test_negative_count_is_rejected():
    payload = valid_payload()
    payload["count"] = -1

    with pytest.raises(ValidationError):
        SensorObservation.model_validate(payload)


def test_unknown_sensor_type_is_rejected():
    payload = valid_payload()
    payload["sensor_type"] = "motorcycle-count"

    with pytest.raises(ValidationError):
        SensorObservation.model_validate(payload)


def test_unknown_intersection_is_rejected():
    payload = valid_payload()
    payload["intersection_id"] = f"unknown-{TEST_INTERSECTION_ID}"

    with pytest.raises(ValidationError):
        SensorObservation.model_validate(payload)


def test_light_from_another_intersection_is_rejected():
    intersections = list(INTERSECTION_LIGHTS)

    if len(intersections) < 2:
        pytest.skip("At least two configured intersections are required")

    first_intersection = intersections[0]
    second_intersection = intersections[1]
    other_lights = INTERSECTION_LIGHTS[second_intersection]

    if not other_lights:
        pytest.skip("Second configured intersection has no lights")

    payload = valid_payload()
    payload["intersection_id"] = first_intersection
    payload["light_id"] = next(iter(other_lights))

    if payload["light_id"] in INTERSECTION_LIGHTS[first_intersection]:
        pytest.skip("Selected light is also assigned to the first intersection")

    with pytest.raises(ValidationError):
        SensorObservation.model_validate(payload)


def test_timezone_is_required():
    payload = valid_payload()
    payload["observed_at"] = "2026-10-19T10:15:30"

    with pytest.raises(ValidationError):
        SensorObservation.model_validate(payload)


def test_unexpected_fields_are_rejected():
    payload = valid_payload()
    payload["unexpected"] = "not allowed"

    with pytest.raises(ValidationError):
        SensorObservation.model_validate(payload)


def test_processed_observation_adds_timestamp():
    observation = ProcessedSensorObservation.model_validate(valid_payload())

    assert observation.processed_at.tzinfo is not None
    assert observation.processed_at.utcoffset() == timezone.utc.utcoffset(
        observation.processed_at
    )


def test_sensor_id_is_not_accepted():
    payload = valid_payload()
    payload["sensor_id"] = "legacy-sensor-id"

    with pytest.raises(ValidationError):
        SensorObservation.model_validate(payload)
