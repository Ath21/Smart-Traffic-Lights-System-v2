# STLS v2 — Development Cheatsheet

## 1. Traffic Count Processor

**Unit tests** (from the service directory):

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/traffic-count-processor
uv run python -m pytest -v
```

**RabbitMQ integration test:**

```bash
uv run python tests/integration_rabbitmq.py
```

**Start the service:**

```bash
uv run uvicorn app.main:app \
  --host 127.0.0.1 \
  --port 8000 \
  --log-level info
```

Health endpoints:

- `http://127.0.0.1:8000/health`
- `http://127.0.0.1:8000/ready`

## 2. Detection Events Processor

**Unit tests** (from the service directory):

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/detection-events-processor
uv run python -m pytest -v
```

**RabbitMQ integration test:**

```bash
# Run from the Detection Events Processor directory.
uv run python scripts/rabbitmq_integration_test.py
```

**Start the service:**

```bash
uv run uvicorn app.main:app \
  --host 127.0.0.1 \
  --port 8001 \
  --log-level info
```

Health endpoints:

- `http://127.0.0.1:8001/health`
- `http://127.0.0.1:8001/ready`

## 3. Traffic Count Generator

**Run from the generators directory:**

```bash
cd ~/Smart-Traffic-Lights-System-v2/generators

uv run python traffic_count_generator.py \
  --duration 60 \
  --interval 5 \
  --seed 42 \
  --at "2026-10-22T08:00:00" \
  --day-type winter
```

## 4. Detection Events Generator

**Run from the generators directory:**

```bash
cd ~/Smart-Traffic-Lights-System-v2/generators

uv run python detection_event_generator.py \
  --duration 60 \
  --interval 5 \
  --seed 42 \
  --at "2026-10-22T08:00:00" \
  --day-type winter
```

## 5. Execution Notes

- Run each long-running service or generator in its own terminal.
- Ensure RabbitMQ and Redis are running before starting the processors.
- Run integration tests only when their required infrastructure is available.
- Run unit tests independently of the live services where possible.
- The generator timestamps use a fixed date for reproducible experiments. Update `--at` when testing another date.
- The `127.0.0.1` binding makes the APIs accessible only from the local machine. Use an appropriate bind address if access from other machines is required.
