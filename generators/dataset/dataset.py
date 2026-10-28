from __future__ import annotations

import json
import math
import random
from dataclasses import dataclass
from datetime import date, datetime
from pathlib import Path
from time import monotonic
from typing import Any, Iterable
from zoneinfo import ZoneInfo


GREECE_TZ = ZoneInfo("Europe/Athens")

# Day-type labels exactly as they appear in the datasets.
DAY_WINTER = "Winter Semester - Regular Teaching Day (Mon-Fri)"
DAY_SPRING = "Spring Semester - Regular Teaching Day (Mon-Fri)"
DAY_EXAMS = "Examination Period (Winter / Spring / September)"
DAY_MAKEUP = "Make-up Classes Period"
DAY_SATURDAY = "Saturday (Limited Campus / Library Operation)"
DAY_EVENT = "Special Campus Event Day (Welcome Day / Open Day)"
DAY_CLOSED = "Sunday & Official Holidays / Vacations (Campus Closed)"

ALL_DAY_TYPES = (
    DAY_WINTER,
    DAY_SPRING,
    DAY_EXAMS,
    DAY_MAKEUP,
    DAY_SATURDAY,
    DAY_EVENT,
    DAY_CLOSED,
)

# Short CLI aliases for --day-type.
DAY_TYPE_ALIASES = {
    "winter": DAY_WINTER,
    "spring": DAY_SPRING,
    "exams": DAY_EXAMS,
    "makeup": DAY_MAKEUP,
    "saturday": DAY_SATURDAY,
    "event": DAY_EVENT,
    "closed": DAY_CLOSED,
}


# --------------------------------------------------------------------------
# Intersections (derived from the dataset, not from environment variables)
# --------------------------------------------------------------------------


@dataclass(frozen=True)
class IntersectionConfig:
    intersection_id: str
    light_ids: tuple[str, ...]


# --------------------------------------------------------------------------
# Academic calendar -> day type
# --------------------------------------------------------------------------


@dataclass(frozen=True)
class CalendarEntry:
    start: date
    end: date
    category: str
    description: str

    def contains(self, day: date) -> bool:
        return self.start <= day <= self.end


class AcademicCalendar:
    """Maps a calendar date to one of the dataset's academic day types."""

    def __init__(self, entries: Iterable[CalendarEntry]) -> None:
        self.entries = tuple(entries)

    @classmethod
    def from_metadata(cls, raw: list[dict[str, Any]]) -> AcademicCalendar:
        return cls(
            CalendarEntry(
                start=date.fromisoformat(item["start"]),
                end=date.fromisoformat(item["end"]),
                category=item["category"],
                description=item.get("description", ""),
            )
            for item in raw
        )

    def _matching(self, day: date, *categories: str) -> list[CalendarEntry]:
        return [
            e
            for e in self.entries
            if e.category in categories and e.contains(day)
        ]

    def day_type_for(self, day: date) -> str:
        """Resolve the day type.

        Precedence: holiday/vacation > special event > weekend >
        exams > make-up > teaching. Dates outside every academic period
        (e.g. mid-summer) fall back to Saturday / closed.
        """
        if self._matching(day, "Holiday", "Vacation"):
            return DAY_CLOSED
        if self._matching(day, "Event"):
            return DAY_EVENT

        weekday = day.weekday()  # Mon=0 ... Sun=6
        if weekday == 6:
            return DAY_CLOSED
        if weekday == 5:
            return DAY_SATURDAY

        if self._matching(day, "Exams"):
            return DAY_EXAMS
        if self._matching(day, "Make-up"):
            return DAY_MAKEUP

        teaching = self._matching(day, "Teaching")
        if teaching:
            if "spring" in teaching[0].description.lower():
                return DAY_SPRING
            return DAY_WINTER

        return DAY_CLOSED


# --------------------------------------------------------------------------
# Simulated clock
# --------------------------------------------------------------------------


class SimulatedClock:
    """Europe/Athens clock that can start at a chosen time and run faster.

    With no start time and speed 1.0 this is just the wall clock.
    """

    def __init__(
        self,
        start: datetime | None = None,
        speed: float = 1.0,
    ) -> None:
        if speed <= 0:
            raise ValueError("speed must be greater than zero")

        if start is None:
            start = datetime.now(GREECE_TZ)
        elif start.tzinfo is None:
            start = start.replace(tzinfo=GREECE_TZ)

        self._start = start.astimezone(GREECE_TZ)
        self._speed = speed
        self._t0 = monotonic()

    def now(self) -> datetime:
        elapsed = (monotonic() - self._t0) * self._speed
        return datetime.fromtimestamp(
            self._start.timestamp() + elapsed, GREECE_TZ
        )


def parse_start_time(value: str) -> datetime:
    """Parse --at; naive values are interpreted as Europe/Athens local time."""
    parsed = datetime.fromisoformat(value)
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=GREECE_TZ)
    return parsed.astimezone(GREECE_TZ)


# --------------------------------------------------------------------------
# Dataset loading
# --------------------------------------------------------------------------


@dataclass(frozen=True)
class LoadedDataset:
    intersections: tuple[IntersectionConfig, ...]
    light_has_public_transport: dict[str, bool]
    calendar: AcademicCalendar
    # (light_id, day_type, hour) -> raw record
    records: dict[tuple[str, str, int], dict[str, Any]]

    def record(
        self,
        light_id: str,
        day_type: str,
        hour: int,
    ) -> dict[str, Any]:
        return self.records[(light_id, day_type, hour)]


def load_dataset(path: str | Path, data_key: str) -> LoadedDataset:
    """Load and validate one STLS dataset file.

    data_key is "counts_data" or "events_data".
    """
    path = Path(path)
    try:
        with path.open(encoding="utf-8") as handle:
            raw = json.load(handle)
    except FileNotFoundError as exc:
        raise RuntimeError(f"Dataset file not found: {path}") from exc

    metadata = raw["metadata"]
    mapping = metadata["stls_light_mapping"]

    # Preserve dataset order; group lights by intersection.
    grouped: dict[str, list[str]] = {}
    has_pt: dict[str, bool] = {}
    for item in mapping:
        light_id = item["stls_light"]
        grouped.setdefault(item["stls_intersection"], []).append(light_id)
        has_pt[light_id] = bool(item.get("has_public_transport", False))

    intersections = tuple(
        IntersectionConfig(intersection_id, tuple(lights))
        for intersection_id, lights in grouped.items()
    )

    for intersection in intersections:
        # build_routing_key rejects dots in intersection IDs.
        if "." in intersection.intersection_id:
            raise RuntimeError(
                f"Intersection ID contains '.': {intersection.intersection_id}"
            )

    records: dict[tuple[str, str, int], dict[str, Any]] = {}
    for row in raw[data_key]:
        key = (
            row["stls_light"],
            row["academic_day_type"],
            int(row["hour_index"]),
        )
        if key in records:
            raise RuntimeError(f"Duplicate dataset record in {path}: {key}")
        records[key] = row

    expected = {
        (light, day_type, hour)
        for light in has_pt
        for day_type in ALL_DAY_TYPES
        for hour in range(24)
    }
    missing = expected - records.keys()
    if missing:
        raise RuntimeError(
            f"{path} is missing {len(missing)} records, e.g. "
            f"{sorted(missing)[0]}"
        )

    return LoadedDataset(
        intersections=intersections,
        light_has_public_transport=has_pt,
        calendar=AcademicCalendar.from_metadata(
            metadata["academic_calendar_schedule"]
        ),
        records=records,
    )


# --------------------------------------------------------------------------
# Sampling helpers
# --------------------------------------------------------------------------


def poisson_sample(rng: random.Random, mean: float) -> int:
    """Sample a Poisson count without third-party numerical libraries."""
    if mean <= 0:
        return 0

    # Knuth's algorithm underflows / gets slow for large means;
    # the normal approximation is accurate there.
    if mean > 30:
        return max(0, round(rng.gauss(mean, math.sqrt(mean))))

    threshold = math.exp(-mean)
    product = 1.0
    count = 0

    while product > threshold:
        count += 1
        product *= rng.random()

    return count - 1


def probability_over_windows(p: float, windows: float) -> float:
    """P(at least one event) over `windows` independent windows of prob p."""
    p = min(max(p, 0.0), 1.0)
    if windows <= 0:
        return 0.0
    return 1.0 - (1.0 - p) ** windows
