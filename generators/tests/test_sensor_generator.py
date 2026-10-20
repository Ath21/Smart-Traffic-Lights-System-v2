
from datetime import datetime
from zoneinfo import ZoneInfo

import pytest
from pydantic import ValidationError

from sensor_data_generator_common import (
    SENSOR_TYPES,
    IntersectionConfig,
    SensorDataGenerator,
    SensorObservation,
    build_routing_key,
)


ATHENS_TZ = ZoneInfo("Europe/Athens")

TEST_INTERSECTIONS = (
    IntersectionConfig(
        intersection_id="ekklisia-3",
        light_ids=("dimitsanas-301S", "edessis-302W", "korytsas-303E"),
    ),
    IntersectionConfig(
        intersection_id="agiou-spyridonos-2",
        light_ids=("agiou-spyridonos201W", "dimitsanas-202N"),
    ),
    IntersectionConfig(
        intersection_id="anatoliki-pyli-1",
        light_ids=("mhlou-101N", "agiou-spyridonos-102W"),
    ),
)


def test_routing_key() -> None:
    assert (
        build_routing_key("ekklisia-3", "vehicle-count")
        == "stls.sensor-data.input.ekklisia-3.vehicle-count"
    )


def test_reject_invalid_sensor_type_in_routing_key() -> None:
    with pytest.raises(ValueError):
        build_routing_key("ekklisia-3", "unknown")


def test_generate_expected_observation_count() -> None:
    generator = SensorDataGenerator(profile="normal", seed=42)

    observations = generator.generate_observations(TEST_INTERSECTIONS)

    # Seven configured lights × three sensor types.
    assert len(observations) == 21


def test_every_observation_has_valid_fields() -> None:
    generator = SensorDataGenerator(profile="normal", seed=42)

    observations = generator.generate_observations(TEST_INTERSECTIONS)

    for observation in observations:
        assert observation.sensor_type in SENSOR_TYPES
        assert observation.count >= 0
        assert observation.observed_at.tzinfo is not None
        assert observation.schema_version == "1.0"
        assert observation.event_id is not None


def test_seed_reproduces_counts() -> None:
    first = SensorDataGenerator(profile="normal", seed=123)
    second = SensorDataGenerator(profile="normal", seed=123)

    first_events = first.generate_observations(TEST_INTERSECTIONS)
    second_events = second.generate_observations(TEST_INTERSECTIONS)

    assert [
        (event.intersection_id, event.light_id, event.sensor_type, event.count)
        for event in first_events
    ] == [
        (event.intersection_id, event.light_id, event.sensor_type, event.count)
        for event in second_events
    ]


def test_reject_naive_timestamp() -> None:
    generator = SensorDataGenerator(profile="normal", seed=42)

    with pytest.raises(ValueError, match="timezone"):
        generator.generate_observations(
            TEST_INTERSECTIONS,
            observed_at=datetime(2026, 10, 20, 12, 0),
        )


def test_schema_rejects_negative_count() -> None:
    with pytest.raises(ValidationError):
        SensorObservation(
            intersection_id="ekklisia-3",
            light_id="dimitsanas-301S",
            sensor_type="vehicle-count",
            count=-1,
            observed_at=datetime.now(ATHENS_TZ),
        )


def test_schema_rejects_unknown_sensor_type() -> None:
    with pytest.raises(ValidationError):
        SensorObservation(
            intersection_id="ekklisia-3",
            light_id="dimitsanas-301S",
            sensor_type="unknown",
            count=1,
            observed_at=datetime.now(ATHENS_TZ),
        )

