from datetime import datetime
from zoneinfo import ZoneInfo

import pytest
from pydantic import ValidationError

from traffic_count_generator_common import (
    SENSOR_TYPES,
    SensorDataGenerator,
    SensorObservation,
    build_routing_key,
    load_traffic_dataset,
)

ATHENS_TZ = ZoneInfo("Europe/Athens")
DAY_TYPE = "Winter Semester - Regular Teaching Day (Mon-Fri)"
TIMESTAMP = datetime(2026, 10, 22, 8, 0, tzinfo=ATHENS_TZ)


@pytest.fixture
def dataset():
    return load_traffic_dataset()


def make_generator(dataset, seed=42, load_factor=1.0):
    return SensorDataGenerator(
        dataset=dataset,
        interval_seconds=30,
        seed=seed,
        load_factor=load_factor,
        day_type_override=DAY_TYPE,
    )


def test_routing_key():
    assert build_routing_key("ekklisia-3", "vehicle-count") == (
        "stls.sensor-data.input.ekklisia-3.vehicle-count"
    )


def test_invalid_routing_key_is_rejected():
    with pytest.raises(ValueError):
        build_routing_key("ekklisia.3", "vehicle-count")

    with pytest.raises(ValueError):
        build_routing_key("ekklisia-3", "unknown")


def test_generate_one_observation_per_light_and_sensor_type(dataset):
    generator = make_generator(dataset)
    observations = generator.generate_observations(TIMESTAMP)

    expected_lights = sum(
        len(intersection.light_ids)
        for intersection in dataset.intersections
    )

    assert len(observations) == expected_lights * len(SENSOR_TYPES)


def test_every_observation_has_valid_fields(dataset):
    observations = make_generator(dataset).generate_observations(TIMESTAMP)

    for observation in observations:
        assert observation.sensor_type in SENSOR_TYPES
        assert observation.count >= 0
        assert observation.observed_at.tzinfo is not None
        assert observation.schema_version == "1.0"
        assert observation.event_id is not None
        assert observation.intersection_id
        assert observation.light_id


def test_seed_reproduces_observations(dataset):
    first = make_generator(dataset, seed=123).generate_observations(TIMESTAMP)
    second = make_generator(dataset, seed=123).generate_observations(TIMESTAMP)

    assert [
        (e.event_id, e.intersection_id, e.light_id, e.sensor_type, e.count)
        for e in first
    ] == [
        (e.event_id, e.intersection_id, e.light_id, e.sensor_type, e.count)
        for e in second
    ]


def test_zero_load_factor_produces_zero_counts(dataset):
    observations = make_generator(
        dataset, load_factor=0
    ).generate_observations(TIMESTAMP)

    assert observations
    assert all(observation.count == 0 for observation in observations)


def test_naive_timestamp_is_rejected(dataset):
    generator = make_generator(dataset)

    with pytest.raises(ValueError, match="timezone"):
        generator.generate_observations(datetime(2026, 10, 22, 8, 0))


def test_invalid_interval_is_rejected(dataset):
    with pytest.raises(ValueError, match="interval_seconds"):
        SensorDataGenerator(dataset=dataset, interval_seconds=0)


def test_negative_load_factor_is_rejected(dataset):
    with pytest.raises(ValueError, match="load_factor"):
        SensorDataGenerator(
            dataset=dataset,
            interval_seconds=30,
            load_factor=-1,
        )


def test_schema_rejects_negative_count():
    with pytest.raises(ValidationError):
        SensorObservation(
            event_id="00000000-0000-4000-8000-000000000001",
            intersection_id="ekklisia-3",
            light_id="dimitsanas-301S",
            sensor_type="vehicle-count",
            count=-1,
            observed_at=TIMESTAMP,
        )


def test_schema_rejects_unknown_sensor_type():
    with pytest.raises(ValidationError):
        SensorObservation(
            event_id="00000000-0000-4000-8000-000000000001",
            intersection_id="ekklisia-3",
            light_id="dimitsanas-301S",
            sensor_type="unknown",
            count=1,
            observed_at=TIMESTAMP,
        )
