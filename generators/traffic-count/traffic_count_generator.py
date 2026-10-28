from __future__ import annotations

import argparse
import asyncio
import logging
import os
import signal
from datetime import datetime
from time import monotonic

import aio_pika
from dotenv import load_dotenv

from generators.traffic_count_generator_common import (
    EXCHANGE_NAME,
    SensorDataGenerator,
    build_routing_key,
    load_traffic_dataset,
)
from generators.dataset import (
    DAY_TYPE_ALIASES,
    SimulatedClock,
    parse_start_time,
)

logger = logging.getLogger(__name__)


async def publish_observations(
    exchange: aio_pika.abc.AbstractExchange,
    generator: SensorDataGenerator,
    observed_at: datetime,
) -> int:
    """Generate and publish one batch of sensor observations."""
    observations = generator.generate_observations(observed_at)

    for observation in observations:
        routing_key = build_routing_key(
            observation.intersection_id,
            observation.sensor_type,
        )

        message = aio_pika.Message(
            body=observation.model_dump_json().encode("utf-8"),
            content_type="application/json",
            delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
            message_id=str(observation.event_id),
            type=observation.sensor_type,
            timestamp=observation.observed_at,
        )

        await exchange.publish(
            message,
            routing_key=routing_key,
            mandatory=True,
        )

        logger.debug(
            "Sensor event published: event_id=%s intersection=%s "
            "light=%s sensor_type=%s count=%s routing_key=%s",
            observation.event_id,
            observation.intersection_id,
            observation.light_id,
            observation.sensor_type,
            observation.count,
            routing_key,
        )

    return len(observations)


async def run(args: argparse.Namespace) -> None:
    load_dotenv()

    rabbitmq_url = os.getenv("RABBITMQ_URL", "").strip()
    if not rabbitmq_url:
        raise RuntimeError("RABBITMQ_URL must be configured")

    exchange_name = os.getenv(
        "RABBITMQ_EXCHANGE_NAME",
        EXCHANGE_NAME,
    ).strip()

    if not exchange_name:
        raise RuntimeError("RABBITMQ_EXCHANGE_NAME must not be empty")

    dataset = load_traffic_dataset(args.dataset)
    if not dataset.intersections:
        raise RuntimeError("The dataset defines no intersections")

    clock = SimulatedClock(
        start=parse_start_time(args.at) if args.at else None,
        speed=args.speed,
    )

    generator = SensorDataGenerator(
        dataset=dataset,
        # Each observation covers `interval` real seconds, which is
        # `interval * speed` seconds of simulated time.
        interval_seconds=args.interval * args.speed,
        seed=args.seed,
        load_factor=args.load_factor,
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
        channel = await connection.channel(
            publisher_confirms=True,
        )

        exchange = await channel.declare_exchange(
            exchange_name,
            aio_pika.ExchangeType.TOPIC,
            durable=True,
        )

        logger.info(
            "Sensor workload generator started: exchange=%s "
            "interval=%ss duration=%s seed=%s intersections=%s "
            "lights=%s load_factor=%s speed=%s day_type_override=%s",
            exchange_name,
            args.interval,
            args.duration if args.duration is not None else "unlimited",
            args.seed,
            len(dataset.intersections),
            sum(len(i.light_ids) for i in dataset.intersections),
            args.load_factor,
            args.speed,
            args.day_type or "calendar",
        )

        while not stop_event.is_set():
            elapsed = monotonic() - started_at

            if (
                args.duration is not None
                and elapsed >= args.duration
            ):
                break

            observed_at = clock.now()

            batch_count = await publish_observations(
                exchange,
                generator,
                observed_at,
            )
            total_published += batch_count

            logger.info(
                "Sensor batch published: batch_count=%s total_published=%s "
                "simulated_time=%s day_type=%s",
                batch_count,
                total_published,
                observed_at.isoformat(timespec="seconds"),
                generator.day_type_at(observed_at),
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
            "Sensor workload generator stopped: total_published=%s "
            "elapsed_seconds=%.2f",
            total_published,
            monotonic() - started_at,
        )

    finally:
        await connection.close()


async def wait_or_stop(
    stop_event: asyncio.Event,
    seconds: float,
) -> None:
    try:
        await asyncio.wait_for(
            stop_event.wait(),
            timeout=seconds,
        )
    except TimeoutError:
        pass


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Generate synthetic STLS sensor observations from the "
            "traffic-counts dataset."
        ),
    )
    parser.add_argument(
        "--dataset",
        default=None,
        help=(
            "Path to dataset/stls_traffic_counts.json (default: "
            "$STLS_TRAFFIC_COUNTS_PATH, else next to this script)."
        ),
    )
    parser.add_argument("--interval", type=float, default=30.0)
    parser.add_argument(
        "--duration",
        type=float,
        default=None,
        help="Duration in seconds; omit to run until interrupted.",
    )
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
        help="Multiply all dataset rates (e.g. 3.0 for stress tests).",
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
        logger.info("Sensor workload generator interrupted")
    except Exception:
        logger.exception("Sensor workload generator failed")
        raise SystemExit(1)


if __name__ == "__main__":
    main()
