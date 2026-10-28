from datetime import datetime
from zoneinfo import ZoneInfo

import pytest
from pydantic import ValidationError

from detection_event_generator_common import (
    DETECTION_TYPES,
    DetectionEvent,
    DetectionEventGenerator,
    build_routing_key,
    load_detection_dataset,
)
from generators.dataset import probability_over_windows

ATHENS_TZ = ZoneInfo("Europe/Athens")
DAY_TYPE = "Winter Semester - Regular Teaching Day (Mon-Fri)"
TIMESTAMP = datetime(2026, 10, 22, 8, 0, tzinfo=ATHENS_TZ)


@pytest.fixture
def dataset():
    return load_detection_dataset()


def make_generator(dataset, seed=42, load_factor=1.0):
    return DetectionEventGenerator(
        dataset=dataset,
        interval_seconds=30,
        seed=seed,
        load_factor=load_factor,
        day_type_override=DAY_TYPE,
    )


def test_routing_key():
    assert build_routing_key("ekklisia-3", "accident") == (
        "stls.detection-events.input.ekklisia-3.accident"
    )


def test_invalid_routing_key_is_rejected():
    with pytest.raises(ValueError):
        build_routing_key("ekklisia.3", "accident")

    with pytest.raises(ValueError):
        build_routing_key("ekklisia-3", "unknown")


def test_generated_events_have_valid_fields(dataset):
    events = make_generator(dataset).generate_events(TIMESTAMP)

    for event in events:
        assert event.schema_version == "1.0"
        assert event.detection_type in DETECTION_TYPES
        assert event.event_id is not None
        assert event.detected_at == TIMESTAMP
        assert event.intersection_id
        assert event.light_id

        assert any(
            event.intersection_id == intersection.intersection_id
            and event.light_id in intersection.light_ids
            for intersection in dataset.intersections
        )


def test_seed_reproduces_event_choices(dataset):
    first = make_generator(dataset, seed=123).generate_events(TIMESTAMP)
    second = make_generator(dataset, seed=123).generate_events(TIMESTAMP)

    assert [
        (e.event_id, e.intersection_id, e.light_id, e.detection_type)
        for e in first
    ] == [
        (e.event_id, e.intersection_id, e.light_id, e.detection_type)
        for e in second
    ]


def test_zero_load_factor_produces_no_events(dataset):
    events = make_generator(
        dataset, load_factor=0
    ).generate_events(TIMESTAMP)

    assert events == []


def test_naive_timestamp_is_rejected(dataset):
    generator = make_generator(dataset)

    with pytest.raises(ValueError, match="timezone"):
        generator.generate_events(datetime(2026, 10, 22, 8, 0))


def test_invalid_interval_is_rejected(dataset):
    with pytest.raises(ValueError, match="interval_seconds"):
        DetectionEventGenerator(dataset=dataset, interval_seconds=0)


def test_invalid_cycle_length_is_rejected(dataset):
    with pytest.raises(ValueError, match="cycle_seconds"):
        DetectionEventGenerator(
            dataset=dataset,
            interval_seconds=30,
            cycle_seconds=0,
        )


def test_negative_load_factor_is_rejected(dataset):
    with pytest.raises(ValueError, match="load_factor"):
        DetectionEventGenerator(
            dataset=dataset,
            interval_seconds=30,
            load_factor=-1,
        )


@pytest.mark.parametrize(
    ("probability", "windows", "expected"),
    [
        (0.0, 1, 0.0),
        (1.0, 1, 1.0),
        (0.5, 0, 0.0),
        (0.5, 2, 0.75),
    ],
)
def test_probability_over_windows(probability, windows, expected):
    assert probability_over_windows(probability, windows) == pytest.approx(
        expected
    )


def test_schema_rejects_unknown_detection_type():
    with pytest.raises(ValidationError):
        DetectionEvent(
            event_id="00000000-0000-4000-8000-000000000001",
            intersection_id="ekklisia-3",
            light_id="dimitsanas-301S",
            detection_type="unknown",
            detected_at=TIMESTAMP,
        )
