from __future__ import annotations

import argparse
import asyncio
import logging
import os
import signal
from pathlib import Path
from datetime import datetime
from time import monotonic

import aio_pika
from dotenv import load_dotenv

from detection_event_generator_common import (
    DEFAULT_SIGNAL_CYCLE_SECONDS,
    EXCHANGE_NAME,
    DetectionEventGenerator,
    build_routing_key,
    load_detection_dataset,
)
from stls_dataset import (
    DAY_TYPE_ALIASES,
    SimulatedClock,
    parse_start_time,
)


logger = logging.getLogger(__name__)


async def publish_detection_events(
    exchange,
    generator: DetectionEventGenerator,
    detected_at: datetime,
) -> int:
    """Generate and publish one batch of detection events."""
    events = generator.generate_events(detected_at)

    for event in events:
        routing_key = build_routing_key(
            event.intersection_id,
            event.detection_type,
        )

        message = aio_pika.Message(
            body=event.model_dump_json().encode("utf-8"),
            content_type="application/json",
            delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
            message_id=str(event.event_id),
            type=event.detection_type,
            timestamp=event.detected_at,
        )

        await exchange.publish(
            message,
            routing_key=routing_key,
            mandatory=True,
        )

        logger.debug(
            "Detection event published: event_id=%s intersection=%s "
            "light=%s detection_type=%s routing_key=%s",
            event.event_id,
            event.intersection_id,
            event.light_id,
            event.detection_type,
            routing_key,
        )

    return len(events)


async def wait_or_stop(
    stop_event: asyncio.Event,
    seconds: float,
) -> None:
    try:
        await asyncio.wait_for(stop_event.wait(), timeout=seconds)
    except TimeoutError:
        pass


async def run(args: argparse.Namespace) -> None:
    load_dotenv(Path(__file__).resolve().parent / ".env", override=False)

    rabbitmq_url = os.getenv("RABBITMQ_URL", "").strip()
    if not rabbitmq_url:
        raise RuntimeError("RABBITMQ_URL must be configured")

    exchange_name = os.getenv(
        "RABBITMQ_EXCHANGE_NAME",
        EXCHANGE_NAME,
    ).strip()

    if not exchange_name:
        raise RuntimeError("RABBITMQ_EXCHANGE_NAME must not be empty")

    dataset = load_detection_dataset(args.dataset)
    if not dataset.intersections:
        raise RuntimeError("The dataset defines no intersections")

    clock = SimulatedClock(
        start=parse_start_time(args.at) if args.at else None,
        speed=args.speed,
    )

    generator = DetectionEventGenerator(
        dataset=dataset,
        # Each batch covers `interval` real seconds, which is
        # `interval * speed` seconds of simulated time.
        interval_seconds=args.interval * args.speed,
        seed=args.seed,
        load_factor=args.load_factor,
        cycle_seconds=args.cycle_seconds,
        day_type_override=args.day_type,
    )

    stop_event = asyncio.Event()
    loop = asyncio.get_running_loop()

    for sig in (signal.SIGINT, signal.SIGTERM):
        try:
            loop.add_signal_handler(sig, stop_event.set)
        except NotImplementedError:
            pass

    connection = await aio_pika.connect_robust(rabbitmq_url)
    total_published = 0
    started_at = monotonic()

    try:
        channel = await connection.channel(publisher_confirms=True)

        exchange = await channel.declare_exchange(
            exchange_name,
            aio_pika.ExchangeType.TOPIC,
            durable=True,
        )

        logger.info(
            "Detection workload generator started: exchange=%s "
            "interval=%ss duration=%s seed=%s intersections=%s "
            "lights=%s load_factor=%s speed=%s cycle_seconds=%s "
            "day_type_override=%s",
            exchange_name,
            args.interval,
            args.duration if args.duration is not None else "unlimited",
            args.seed,
            len(dataset.intersections),
            sum(len(i.light_ids) for i in dataset.intersections),
            args.load_factor,
            args.speed,
            args.cycle_seconds,
            args.day_type or "calendar",
        )

        while not stop_event.is_set():
            if (
                args.duration is not None
                and monotonic() - started_at >= args.duration
            ):
                break

            detected_at = clock.now()

            batch_count = await publish_detection_events(
                exchange,
                generator,
                detected_at,
            )
            total_published += batch_count

            logger.info(
                "Detection batch published: batch_count=%s "
                "total_published=%s simulated_time=%s day_type=%s",
                batch_count,
                total_published,
                detected_at.isoformat(timespec="seconds"),
                generator.day_type_at(detected_at),
            )

            if args.duration is None:
                await wait_or_stop(stop_event, args.interval)
            else:
                remaining = args.duration - (monotonic() - started_at)
                if remaining <= 0:
                    break
                await wait_or_stop(
                    stop_event,
                    min(args.interval, remaining),
                )

        logger.info(
            "Detection workload generator stopped: total_published=%s "
            "elapsed_seconds=%.2f",
            total_published,
            monotonic() - started_at,
        )
    finally:
        await connection.close()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Generate synthetic STLS detection events from the "
            "detection-events dataset."
        )
    )
    parser.add_argument(
        "--dataset",
        default=None,
        help=(
            "Path to stls_detection_events.json (default: "
            "$STLS_DETECTION_EVENTS_PATH, else next to this script)."
        ),
    )
    parser.add_argument("--interval", type=float, default=30.0)
    parser.add_argument("--duration", type=float, default=None)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument(
        "--at",
        default=None,
        help=(
            "Simulated start time, ISO 8601 (e.g. 2026-11-03T08:00). "
            "Naive values are Europe/Athens local time. "
            "Default: the current time."
        ),
    )
    parser.add_argument(
        "--speed",
        type=float,
        default=1.0,
        help="Simulated seconds per real second (default: 1.0).",
    )
    parser.add_argument(
        "--day-type",
        choices=tuple(DAY_TYPE_ALIASES),
        default=None,
        help="Force a day type instead of deriving it from the calendar.",
    )
    parser.add_argument(
        "--load-factor",
        type=float,
        default=1.0,
        help="Multiply all dataset probabilities (e.g. 5.0 for stress tests).",
    )
    parser.add_argument(
        "--cycle-seconds",
        type=float,
        default=DEFAULT_SIGNAL_CYCLE_SECONDS,
        help=(
            "Signal cycle length used to convert per-cycle probabilities "
            f"(default: {DEFAULT_SIGNAL_CYCLE_SECONDS:g})."
        ),
    )

    args = parser.parse_args()

    if args.interval <= 0:
        parser.error("--interval must be greater than zero")
    if args.duration is not None and args.duration <= 0:
        parser.error("--duration must be greater than zero")
    if args.speed <= 0:
        parser.error("--speed must be greater than zero")
    if args.load_factor < 0:
        parser.error("--load-factor must not be negative")
    if args.cycle_seconds <= 0:
        parser.error("--cycle-seconds must be greater than zero")

    if args.at:
        try:
            parse_start_time(args.at)
        except ValueError:
            parser.error("--at must be an ISO 8601 date/time")

    if args.day_type:
        args.day_type = DAY_TYPE_ALIASES[args.day_type]

    return args


def main() -> None:
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s %(levelname)s %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )

    try:
        asyncio.run(run(parse_args()))
    except KeyboardInterrupt:
        logger.info("Detection workload generator interrupted")
    except Exception:
        logger.exception("Detection workload generator failed")
        raise SystemExit(1)


if __name__ == "__main__":
    main()
