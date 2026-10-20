#!/usr/bin/env bash

set -Eeuo pipefail

# Resolve the repository root from this script's location.
ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

NETWORK="stls-monitoring"

CONTAINERS=(
    stls-rabbitmq
    stls-redis
    stls-redis-insight
    stls-prometheus
    stls-grafana
)

VOLUMES=(
    stls-rabbitmq-data
    stls-redis-data
    stls-redis-insight-data
    stls-prometheus-data
    stls-grafana-data

    # Anonymous volumes from the previous infrastructure setup.
    72d9d781a63c8a156e1292f1f56d2fb8f03100f26d16e242f7b52fa5b305ef
    9c9a6a905436542e4dca4ac77fabb39b4cb7bcf9a985387ecab998fb7bc65428
)

require_docker() {
    if ! command -v docker >/dev/null 2>&1; then
        echo "ERROR: Docker is not installed or not in PATH." >&2
        exit 1
    fi

    if ! docker info >/dev/null 2>&1; then
        echo "ERROR: Docker is unavailable. Check that Docker is running and you have permission." >&2
        exit 1
    fi
}

down() {
    echo "==> Stopping and removing STLS infrastructure containers..."

    docker rm -f "${CONTAINERS[@]}" 2>/dev/null || true

    echo "==> Removing STLS infrastructure volumes..."

    for volume in "${VOLUMES[@]}"; do
        if docker volume inspect "$volume" >/dev/null 2>&1; then
            docker volume rm "$volume"
        fi
    done

    echo "==> Infrastructure is down."
    echo "    Docker network '$NETWORK' has been preserved."
}

up() {
    echo "==> Creating Prometheus configuration..."

    mkdir -p "$ROOT_DIR/monitoring"

    cat > "$ROOT_DIR/monitoring/prometheus.yml" <<'PROMETHEUS'
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: prometheus
    static_configs:
      - targets: ["localhost:9090"]
PROMETHEUS

    echo "==> Ensuring Docker network exists..."

    if ! docker network inspect "$NETWORK" >/dev/null 2>&1; then
        docker network create "$NETWORK"
    fi

    echo "==> Creating persistent volumes..."

    for volume in \
        stls-rabbitmq-data \
        stls-redis-data \
        stls-redis-insight-data \
        stls-prometheus-data \
        stls-grafana-data
    do
        docker volume create "$volume" >/dev/null
    done

    echo "==> Starting RabbitMQ..."

    docker run -d \
        --name stls-rabbitmq \
        --restart unless-stopped \
        --network "$NETWORK" \
        -p 5672:5672 \
        -p 127.0.0.1:15672:15672 \
        -e RABBITMQ_DEFAULT_USER=ath \
        -e RABBITMQ_DEFAULT_PASS=ath_password \
        -v stls-rabbitmq-data:/var/lib/rabbitmq \
        rabbitmq:4-management

    echo "==> Starting Redis..."

    docker run -d \
        --name stls-redis \
        --restart unless-stopped \
        --network "$NETWORK" \
        -p 127.0.0.1:6379:6379 \
        -v stls-redis-data:/data \
        redis:7-alpine

    echo "==> Starting Redis Insight..."

    docker run -d \
        --name stls-redis-insight \
        --restart unless-stopped \
        --network "$NETWORK" \
        -p 127.0.0.1:5540:5540 \
        -v stls-redis-insight-data:/data \
        redis/redisinsight:latest

    echo "==> Starting Prometheus..."

    docker run -d \
        --name stls-prometheus \
        --restart unless-stopped \
        --network "$NETWORK" \
        -p 127.0.0.1:9090:9090 \
        -v stls-prometheus-data:/prometheus \
        -v "$ROOT_DIR/monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro" \
        prom/prometheus \
        --config.file=/etc/prometheus/prometheus.yml \
        --storage.tsdb.path=/prometheus

    echo "==> Starting Grafana..."

    docker run -d \
        --name stls-grafana \
        --restart unless-stopped \
        --network "$NETWORK" \
        -p 127.0.0.1:3000:3000 \
        -e GF_SECURITY_ADMIN_USER=admin \
        -e GF_SECURITY_ADMIN_PASSWORD=stls_grafana_dev_2026 \
        -v stls-grafana-data:/var/lib/grafana \
        grafana/grafana

    echo
    echo "==> Containers started. Services may need a short time to become ready."
    status
}

status() {
    echo "==> Infrastructure container status:"
    docker ps -a \
        --filter "name=^stls-rabbitmq$" \
        --filter "name=^stls-redis$" \
        --filter "name=^stls-redis-insight$" \
        --filter "name=^stls-prometheus$" \
        --filter "name=^stls-grafana$" \
        --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}'

    echo
    echo "==> Health checks (services may still be starting):"

    if docker exec stls-rabbitmq rabbitmq-diagnostics -q ping >/dev/null 2>&1; then
        echo "RabbitMQ:   OK"
    else
        echo "RabbitMQ:   NOT READY"
    fi

    if docker exec stls-redis redis-cli ping 2>/dev/null | grep -q PONG; then
        echo "Redis:      OK"
    else
        echo "Redis:      NOT READY"
    fi

    if curl -fsS http://127.0.0.1:9090/-/ready >/dev/null 2>&1; then
        echo "Prometheus: OK"
    else
        echo "Prometheus: NOT READY"
    fi

    if curl -fsS http://127.0.0.1:3000/api/health >/dev/null 2>&1; then
        echo "Grafana:    OK"
    else
        echo "Grafana:    NOT READY"
    fi
}

usage() {
    cat <<'USAGE'
Usage: ./scripts/infrastructure.sh [OPTION]

Options:
  --up       Create volumes and start all infrastructure containers
  --down     Remove infrastructure containers and their persistent volumes
  --status   Show container status and basic health checks
  --help     Show this help

Examples:
  ./scripts/infrastructure.sh --up
  ./scripts/infrastructure.sh --status
  ./scripts/infrastructure.sh --down

WARNING: --down permanently deletes the listed persistent volumes.
USAGE
}

main() {
    if [[ $# -ne 1 ]]; then
        usage
        exit 2
    fi

    require_docker

    case "$1" in
        --up)
            up
            ;;
        --down)
            echo "WARNING: This deletes infrastructure data stored in its volumes."
            read -r -p "Type 'yes' to continue: " confirmation
            if [[ "$confirmation" != "yes" ]]; then
                echo "Cancelled."
                exit 0
            fi
            down
            ;;
        --status)
            status
            ;;
        --help|-h)
            usage
            ;;
        *)
            echo "ERROR: Unknown option: $1" >&2
            usage
            exit 2
            ;;
    esac
}

main "$@"
