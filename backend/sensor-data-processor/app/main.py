import asyncio

from app.messaging.consumer import run_consumer


if __name__ == "__main__":
    asyncio.run(run_consumer())
