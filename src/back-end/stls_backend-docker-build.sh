#!/bin/bash
set -e

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_USERNAME="ath21"

# ============================================================
# Microservice Dockerfile Paths
# ============================================================
declare -A SERVICES_PATHS=(
    [user-api]="UserLayer/Services/UserService/Docker/Dockerfile"
    [notification-api]="UserLayer/Services/NotificationService/Docker/Dockerfile"
    [log-api]="LogLayer/Services/LogService/Docker/Dockerfile"
    [traffic-analytics]="TrafficLayer/Services/TrafficAnalyticsService/Docker/Dockerfile"
    [traffic-light-controller]="TrafficLayer/Services/TrafficLightControllerService/Docker/Dockerfile"
    [traffic-light-coordinator]="TrafficLayer/Services/TrafficLightCoordinatorService/Docker/Dockerfile"
    [intersection-controller]="TrafficLayer/Services/IntersectionControllerService/Docker/Dockerfile"
    [sensor-api]="SensorLayer/Services/SensorService/Docker/Dockerfile"
    [detection-api]="SensorLayer/Services/DetectionService/Docker/Dockerfile"
)

usage() {
    echo "Usage: $0 [--all] [--infra] [service...]"
    echo ""
    echo "Options:"
    echo "  --all        Build and push all microservices"
    echo "  --infra      Pull, tag and push all database/broker images"
    echo "  service...   Build and push one or more specific services"
    echo ""
    echo "Available services:"
    for s in "${!SERVICES_PATHS[@]}"; do
        echo "  - $s"
    done
    echo ""
    echo "Examples:"
    echo "  ./stls_backend-docker-build.sh --all"
    echo "  ./stls_backend-docker-build.sh --infra"
    echo "  ./stls_backend-docker-build.sh sensor-api user-api notification-api"
}

# ============================================================
# Build and Push for Application Services
# ============================================================
build_and_push_service() {
    local service="$1"
    local dockerfile="${SERVICES_PATHS[$service]}"
    local image="${DOCKER_USERNAME}/stls-services:${service}"

    if [[ -z "$dockerfile" ]]; then
        echo "Unknown service: $service"
        exit 1
    fi

    local path="$ROOT_DIR/$dockerfile"
    if [[ -f "$path" ]]; then
        echo "Building image for $service..."
        docker build -t "$image" -f "$path" "$ROOT_DIR"
        echo "Pushing image for $service to Docker Hub..."
        docker push "$image"
    else
        echo "No Dockerfile found at $path"
    fi
}

# ============================================================
# Pull, Tag, and Push Infrastructure Images
# ============================================================
push_infra_images() {
    echo "=============================="
    echo "Pulling and tagging infrastructure images..."
    echo "=============================="

    docker pull mongo:6.0
    docker pull redis:7.4
    docker pull postgres:16
    docker pull mcr.microsoft.com/mssql/server:2022-latest
    docker pull rabbitmq:3.8-management
    docker pull portainer/portainer-ce:latest

    echo "Tagging and pushing database and broker images..."

    # Mongo-based
    docker tag mongo:6.0 ${DOCKER_USERNAME}/stls-databases:log-mongo
    docker tag mongo:6.0 ${DOCKER_USERNAME}/stls-databases:notification-mongo
    docker tag mongo:6.0 ${DOCKER_USERNAME}/stls-databases:detection-mongo

    # Redis-based
    docker tag redis:7.4 ${DOCKER_USERNAME}/stls-databases:traffic-light-redis
    docker tag redis:7.4 ${DOCKER_USERNAME}/stls-databases:detection-redis

    # MSSQL-based
    docker tag mcr.microsoft.com/mssql/server:2022-latest ${DOCKER_USERNAME}/stls-databases:traffic-light-mssql
    docker tag mcr.microsoft.com/mssql/server:2022-latest ${DOCKER_USERNAME}/stls-databases:user-mssql

    # PostgreSQL
    docker tag postgres:16 ${DOCKER_USERNAME}/stls-databases:traffic-analytics-postgres

    # RabbitMQ
    docker tag rabbitmq:3.8-management ${DOCKER_USERNAME}/stls-broker:rabbitmq

    # Portainer
    docker tag portainer/portainer-ce:latest ${DOCKER_USERNAME}/stls-monitor:portainer

    # Push all images
    docker push ${DOCKER_USERNAME}/stls-databases:log-mongo
    docker push ${DOCKER_USERNAME}/stls-databases:notification-mongo
    docker push ${DOCKER_USERNAME}/stls-databases:detection-mongo
    docker push ${DOCKER_USERNAME}/stls-databases:traffic-light-redis
    docker push ${DOCKER_USERNAME}/stls-databases:detection-redis
    docker push ${DOCKER_USERNAME}/stls-databases:traffic-light-mssql
    docker push ${DOCKER_USERNAME}/stls-databases:user-mssql
    docker push ${DOCKER_USERNAME}/stls-databases:traffic-analytics-postgres
    docker push ${DOCKER_USERNAME}/stls-broker:rabbitmq
    docker push ${DOCKER_USERNAME}/stls-monitor:portainer

    echo "Infrastructure images pushed successfully."
}

# ============================================================
# Main Logic
# ============================================================
main() {
    if [[ $# -eq 0 ]]; then
        usage
        exit 1
    fi

    case "$1" in
        --all)
            for service in "${!SERVICES_PATHS[@]}"; do
                build_and_push_service "$service"
            done
            ;;
        --infra)
            push_infra_images
            ;;
        *)
            for service in "$@"; do
                build_and_push_service "$service"
            done
            ;;
    esac

    echo "Done."
}

main "$@"
