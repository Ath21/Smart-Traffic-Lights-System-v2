
from __future__ import annotations

import argparse
import asyncio
import logging
import os
import signal
from time import monotonic

import aio_pika
from dotenv import load_dotenv

from sensor_data_generator_common import (
    EXCHANGE_NAME,
    SensorDataGenerator,
    build_routing_key,
    load_intersections,
)

logger = logging.getLogger(__name__)


async def publish_observations(
    exchange: aio_pika.abc.AbstractExchange,
    generator: SensorDataGenerator,
    intersections,
) -> int:
    """Generate and publish one batch of sensor observations."""
    observations = generator.generate_observations(intersections)

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

    intersections = load_intersections()
    if not intersections:
        raise RuntimeError("No intersections are configured")

    generator = SensorDataGenerator(
        profile=args.profile,
        seed=args.seed,
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
            "profile=%s interval=%ss duration=%s seed=%s intersections=%s",
            exchange_name,
            args.profile,
            args.interval,
            args.duration if args.duration is not None else "unlimited",
            args.seed,
            len(intersections),
        )

        while not stop_event.is_set():
            elapsed = monotonic() - started_at

            if (
                args.duration is not None
                and elapsed >= args.duration
            ):
                break

            batch_count = await publish_observations(
                exchange,
                generator,
                intersections,
            )
            total_published += batch_count

            logger.info(
                "Sensor batch published: batch_count=%s total_published=%s",
                batch_count,
                total_published,
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
        description="Generate synthetic STLS sensor observations.",
    )
    parser.add_argument(
        "--profile",
        choices=("quiet", "normal", "peak", "overload"),
        default="normal",
    )
    parser.add_argument("--interval", type=float, default=30.0)
    parser.add_argument(
        "--duration",
        type=float,
        default=None,
        help="Duration in seconds; omit to run until interrupted.",
    )
    parser.add_argument("--seed", type=int, default=42)

    args = parser.parse_args()

    if args.interval <= 0:
        parser.error("--interval must be greater than zero")

    if args.duration is not None and args.duration <= 0:
        parser.error("--duration must be greater than zero")

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
