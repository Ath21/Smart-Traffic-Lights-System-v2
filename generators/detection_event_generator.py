from __future__ import annotations

import argparse
import asyncio
import logging
import os
import signal
from time import monotonic

import aio_pika
from dotenv import load_dotenv

from detection_event_generator_common import (
    EXCHANGE_NAME,
    DetectionEventGenerator,
    build_routing_key,
    load_intersections,
)

from dotenv import load_dotenv

load_dotenv(".env.detection")

logger = logging.getLogger(__name__)


async def publish_detection_events(
    exchange,
    generator: DetectionEventGenerator,
    intersections,
) -> int:
    """Generate and publish one batch of detection events."""
    events = generator.generate_events(intersections)

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

    generator = DetectionEventGenerator(
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
        channel = await connection.channel(publisher_confirms=True)

        exchange = await channel.declare_exchange(
            exchange_name,
            aio_pika.ExchangeType.TOPIC,
            durable=True,
        )

        logger.info(
            "Detection workload generator started: exchange=%s "
            "profile=%s interval=%ss duration=%s seed=%s intersections=%s",
            exchange_name,
            args.profile,
            args.interval,
            args.duration if args.duration is not None else "unlimited",
            args.seed,
            len(intersections),
        )

        while not stop_event.is_set():
            if (
                args.duration is not None
                and monotonic() - started_at >= args.duration
            ):
                break

            batch_count = await publish_detection_events(
                exchange,
                generator,
                intersections,
            )
            total_published += batch_count

            logger.info(
                "Detection batch published: batch_count=%s "
                "total_published=%s",
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
            "Detection workload generator stopped: total_published=%s "
            "elapsed_seconds=%.2f",
            total_published,
            monotonic() - started_at,
        )
    finally:
        await connection.close()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate synthetic STLS detection events."
    )
    parser.add_argument(
        "--profile",
        choices=("quiet", "normal", "peak", "overload"),
        default="normal",
    )
    parser.add_argument("--interval", type=float, default=30.0)
    parser.add_argument("--duration", type=float, default=None)
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
        logger.info("Detection workload generator interrupted")
    except Exception:
        logger.exception("Detection workload generator failed")
        raise SystemExit(1)


if __name__ == "__main__":
    main()
