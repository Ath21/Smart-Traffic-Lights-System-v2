
from __future__ import annotations

import argparse
import asyncio
import os
import signal
from time import monotonic

import aio_pika
from dotenv import load_dotenv

from sensor_data_generator_common import (
    EXCHANGE_NAME,
    SENSOR_TYPES,
    SensorDataGenerator,
    build_routing_key,
    load_intersections,
)


async def publish_observations(
    exchange: aio_pika.abc.AbstractExchange,
    generator: SensorDataGenerator,
    intersections,
) -> int:
    """Generate and publish one batch of sensor observations."""
    observations = generator.generate_observations(intersections)
    published = 0

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

        published += 1
        print(
            f"Published {routing_key} "
            f"light={observation.light_id} "
            f"count={observation.count} "
            f"event_id={observation.event_id}"
        )

    return published


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

    try:
        channel = await connection.channel(publisher_confirms=True)

        exchange = await channel.declare_exchange(
            exchange_name,
            aio_pika.ExchangeType.TOPIC,
            durable=True,
        )

        started_at = monotonic()
        total_published = 0

        print(f"Exchange: {exchange_name}")
        print(f"Profile: {args.profile}")
        print(f"Interval: {args.interval}s")
        print(f"Seed: {args.seed}")
        print("Press Ctrl+C to stop.")

        while not stop_event.is_set():
            if (
                args.duration is not None
                and monotonic() - started_at >= args.duration
            ):
                break

            batch_count = await publish_observations(
                exchange,
                generator,
                intersections,
            )
            total_published += batch_count

            print(
                f"Batch complete: {batch_count} messages; "
                f"total published: {total_published}"
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

        print(f"Stopped. Total published: {total_published}")

    finally:
        await connection.close()


async def wait_or_stop(
    stop_event: asyncio.Event,
    seconds: float,
) -> None:
    try:
        await asyncio.wait_for(stop_event.wait(), timeout=seconds)
    except TimeoutError:
        pass


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate synthetic STLS sensor observations."
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


if __name__ == "__main__":
    try:
        asyncio.run(run(parse_args()))
    except KeyboardInterrupt:
        pass
    except Exception as exc:
        raise SystemExit(f"Sensor generator failed: {exc}") from exc

