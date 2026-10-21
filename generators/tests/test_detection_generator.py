from datetime import datetime
from zoneinfo import ZoneInfo

import pytest
from pydantic import ValidationError

from detection_event_generator_common import (
    DETECTION_TYPES,
    DetectionEvent,
    DetectionEventGenerator,
    IntersectionConfig,
    build_routing_key,
)


ATHENS_TZ = ZoneInfo("Europe/Athens")

TEST_INTERSECTIONS = (
    IntersectionConfig(
        "ekklisia-3",
        ("dimitsanas-301S", "edessis-302W", "korytsas-303E"),
    ),
    IntersectionConfig(
        "agiou-spyridonos-2",
        ("agiou-spyridonos201W", "dimitsanas-202N"),
    ),
    IntersectionConfig(
        "anatoliki-pyli-1",
        ("mhlou-101N", "agiou-spyridonos-102W"),
    ),
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


@pytest.mark.parametrize(
    ("profile", "minimum", "maximum"),
    [
        ("quiet", 1, 2),
        ("normal", 2, 4),
        ("peak", 5, 8),
        ("overload", 10, 15),
    ],
)
def test_profile_controls_batch_size(profile, minimum, maximum):
    generator = DetectionEventGenerator(profile=profile, seed=42)
    events = generator.generate_events(TEST_INTERSECTIONS)

    assert minimum <= len(events) <= maximum


def test_generated_events_have_valid_fields():
    generator = DetectionEventGenerator(profile="overload", seed=42)
    events = generator.generate_events(TEST_INTERSECTIONS)

    assert events

    for event in events:
        assert event.schema_version == "1.0"
        assert event.detection_type in DETECTION_TYPES
        assert event.event_id is not None
        assert event.detected_at.tzinfo is not None

        intersection = next(
            item for item in TEST_INTERSECTIONS
            if item.intersection_id == event.intersection_id
        )
        assert event.light_id in intersection.light_ids


def test_seed_reproduces_event_choices():
    first = DetectionEventGenerator(profile="overload", seed=123)
    second = DetectionEventGenerator(profile="overload", seed=123)

    first_events = first.generate_events(TEST_INTERSECTIONS)
    second_events = second.generate_events(TEST_INTERSECTIONS)

    assert [
        (e.intersection_id, e.light_id, e.detection_type)
        for e in first_events
    ] == [
        (e.intersection_id, e.light_id, e.detection_type)
        for e in second_events
    ]


def test_naive_timestamp_is_rejected():
    generator = DetectionEventGenerator(seed=42)

    with pytest.raises(ValueError, match="timezone"):
        generator.generate_events(
            TEST_INTERSECTIONS,
            detected_at=datetime(2026, 10, 20, 12, 0),
        )


def test_unknown_detection_type_is_rejected():
    with pytest.raises(ValidationError):
        DetectionEvent(
            intersection_id="ekklisia-3",
            light_id="dimitsanas-301S",
            detection_type="unknown",
            detected_at=datetime.now(ATHENS_TZ),
        )


def test_empty_intersection_list_is_rejected():
    generator = DetectionEventGenerator(seed=42)

    with pytest.raises(ValueError, match="At least one intersection"):
        generator.generate_events(())
