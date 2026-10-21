# Sensor Data Processor
uv run uvicorn app.main:app \
  --host 127.0.0.1 \
  --port 8000 \
  --log-level info

# Sensor Data Generator
uv run python sensor_data_generator.py \
  --profile normal \
  --interval 5 \
  --duration 15

# Detection Events Processor
uv run uvicorn app.main:app \
  --host 127.0.0.1 \
  --port 8001 \
  --log-level info

# Detection Events Generator
uv run python detection_event_generator.py \
  --profile normal \
  --interval 5 \
  --duration 60 \
  --seed 42