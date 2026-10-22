# STLS v2 - Cheatsheet
## Phase Development > 1 VM with Docker

### 1. Traffic Count Processor

**Unit tests**

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/traffic-count-processor
uv run python -m pytest -v
```

**RabbitMQ integration test**

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/traffic-count-processor
uv run python tests/scripts/rabbitmq_integration_test.py
```

**Start the service**

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/traffic-count-processor
uv run python -m uvicorn app.main:app --host 127.0.0.1 --port 8000 --log-level info
```

**Health endpoints:**
- `http://127.0.0.1:8000/health`
- `http://127.0.0.1:8000/ready`

---

### 2. Detection Events Processor

**Unit tests**

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/detection-events-processor
uv run python -m pytest -v
```

**RabbitMQ integration test**

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/detection-events-processor
uv run python scripts/rabbitmq_integration_test.py
```

**Start the service**

```bash
cd ~/Smart-Traffic-Lights-System-v2/backend/detection-events-processor
uv run python -m uvicorn app.main:app --host 127.0.0.1 --port 8001 --log-level info
```

**Health endpoints:**
- `http://127.0.0.1:8001/health`
- `http://127.0.0.1:8001/ready`

---

### 3. Traffic Count Generator

```bash
cd ~/Smart-Traffic-Lights-System-v2/generators/traffic-count
uv run python traffic_count_generator.py \
  --duration 60 \
  --interval 5 \
  --seed 42 \
  --at "2026-10-22T08:00:00" \
  --day-type winter
```

---

### 4. Detection Events Generator

```bash
cd ~/Smart-Traffic-Lights-System-v2/generators/detection-events
uv run python detection_event_generator.py \
  --duration 60 \
  --interval 5 \
  --seed 42 \
  --at "2026-10-22T08:00:00" \
  --day-type winter
```