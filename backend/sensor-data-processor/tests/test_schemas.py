
from datetime import datetime, timezone
from uuid import uuid4

import pytest
from pydantic import ValidationError

from app.schemas import ProcessedSensorObservation, SensorObservation


def valid_payload() -> dict:
    return {
        "schema_version": "1.0",
        "event_id": str(uuid4()),
        "intersection_id": "ekklisia-3",
        "sensor_type": "vehicle-count",
        "sensor_id": "sensor-ekklisia-3-vehicle-01",
        "count": 12,
        "observed_at": "2026-10-19T10:15:30Z",
    }


def test_valid_sensor_observation():
    observation = SensorObservation.model_validate(valid_payload())

    assert observation.intersection_id == "ekklisia-3"
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
    payload["intersection_id"] = "unknown-intersection"

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

