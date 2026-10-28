import os
import subprocess
import sys
from pathlib import Path

import pytest


PROJECT_ROOT = Path(__file__).resolve().parents[2]
INTEGRATION_SCRIPT = (
    PROJECT_ROOT / "tests" / "scripts" / "rabbitmq_integration_test.py"
)

pytestmark = [
    pytest.mark.integration,
    pytest.mark.skipif(
        os.getenv("RUN_RABBITMQ_INTEGRATION") != "1",
        reason="Set RUN_RABBITMQ_INTEGRATION=1 to run broker integration tests",
    ),
]


def test_sensor_observation_is_processed_through_rabbitmq():
    result = subprocess.run(
        [sys.executable, str(INTEGRATION_SCRIPT)],
        cwd=PROJECT_ROOT,
        capture_output=True,
        text=True,
        timeout=20,
        check=False,
    )

    output = result.stdout + "\n" + result.stderr

    assert result.returncode == 0, (
        "RabbitMQ end-to-end integration script failed:\n" + output
    )
    assert "PASS: Processed event received." in result.stdout
    assert "Processed at:" in result.stdout
