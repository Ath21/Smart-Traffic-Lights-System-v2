# STLS v2 - Development Cheatsheet

## Phase 1: Development on One VM with Docker

### 1. Traffic Count Processor

**RabbitMQ and Redis integration tests**

Ensure the service's `.env` is configured and RabbitMQ/Redis are running.

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/traffic-count-processor

RUN_RABBITMQ_INTEGRATION=1 uv run pytest -v -m integration
```

**Start the service**

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/traffic-count-processor

uv run python -m uvicorn app.main:app \
  --host 127.0.0.1 --port 8000 --log-level info
```

Health endpoints:

- http://127.0.0.1:8000/health
- http://127.0.0.1:8000/ready

### 2. Detection Events Processor

**RabbitMQ and Redis integration tests**

Ensure the service's `.env` is configured and RabbitMQ/Redis are running.

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/detection-events-processor

RUN_RABBITMQ_INTEGRATION=1 uv run pytest -v -m integration
```

**Start the service**

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/detection-events-processor

uv run python -m uvicorn app.main:app \
  --host 127.0.0.1 --port 8001 --log-level info
```

Health endpoints:

- http://127.0.0.1:8001/health
- http://127.0.0.1:8001/ready

### 3. Traffic Count Generator

Check supported CLI options with `--help` before running. Example simulation:

```bash
cd ~/Smart-Traffic-Lights-System-v2/generators/traffic-count

uv run python traffic_count_generator.py \
  --duration 60 \
  --interval 5 \
  --seed 42 \
  --at "2026-10-28T08:00:00" \
  --day-type winter
```

### 4. Detection Events Generator

Check supported CLI options with `--help` before running. Example simulation:

```bash
cd ~/Smart-Traffic-Lights-System-v2/generators/detection-events

uv run python detection_event_generator.py \
  --duration 60 \
  --interval 5 \
  --seed 42 \
  --at "2026-10-28T08:00:00" \
  --day-type winter
```

The timestamp is an example simulation time. Change it to the scenario you want to test.

### 5. Run All Integration Tests

The integration tests are opt-in and require the appropriate environment variables plus reachable RabbitMQ and Redis services.

```bash
cd ~/Smart-Traffic-Lights-System-v2

(
  cd backend/detection-events-processor &&
  RUN_RABBITMQ_INTEGRATION=1 uv run pytest -v -m integration
) &&
(
  cd backend/traffic-count-processor &&
  RUN_RABBITMQ_INTEGRATION=1 uv run pytest -v -m integration
) &&
(
  cd generators/detection-events &&
  RUN_RABBITMQ_INTEGRATION=1 uv run pytest -v -m integration
) &&
(
  cd generators/traffic-count &&
  RUN_RABBITMQ_INTEGRATION=1 uv run pytest -v -m integration
)
```

The current integration suite contains eight tests across the four components.

### 6. End-to-End Smoke Test Checklist

- [ ] RabbitMQ and Redis are running and reachable from the application environment.
- [ ] Both processors start without configuration or connection errors.
- [ ] `/health` and `/ready` return the expected responses for each processor.
- [ ] Both generators publish events successfully.
- [ ] RabbitMQ routes messages to the expected processor queues.
- [ ] Processor logs confirm valid messages are processed and published.
- [ ] Redis contains the expected processed-event and deduplication records.
- [ ] Replaying an event follows the expected deduplication behavior.
- [ ] All eight integration tests pass.

**Important:** The integration tests verify the RabbitMQ/Redis interactions covered by those tests. They do not alone prove every deployment, recovery, or end-to-end scenario works.
