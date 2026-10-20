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