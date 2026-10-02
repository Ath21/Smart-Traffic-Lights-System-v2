#!/bin/bash
set -e

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
NAMESPACE="uniwa-stls"

LAYERS=("message" "sensor" "traffic" "user" "log")

usage() {
  echo "Usage: $0 {up|down|reset} {/all|/message|/sensor|/traffic|/user|/log}"
  echo ""
  echo "Commands:"
  echo "  up       Start Minikube (with ingress) and deploy selected layers"
  echo "  down     Stop Minikube cluster (pods paused, not deleted)"
  echo "  reset    Delete and recreate Minikube cluster, then redeploy"
  echo ""
  echo "Layers:"
  echo "  /all      All layers in the correct order"
  echo "  /message  Messaging layer (RabbitMQ)"
  echo "  /sensor   Sensor layer (sensors + detection)"
  echo "  /traffic  Traffic layer (controllers + analytics)"
  echo "  /user     User layer (User + Notification)"
  echo "  /log      Log layer"
  echo ""
  echo "Examples:"
  echo "  ./stls_backend-k8s.sh up /all"
  echo "  ./stls_backend-k8s.sh down /traffic"
}

# -----------------------
# Parse arguments
# -----------------------
ACTION=$1
LAYER=${2#/}  # remove leading slash

if [[ -z "$ACTION" ]]; then
  usage
  exit 1
fi

# -----------------------
# Start / Stop / Reset Minikube
# -----------------------
case "$ACTION" in
  up)
    echo "🚀 Starting Minikube with Ingress, Metrics Server, and Headlamp..."
    minikube start --driver=docker --addons=ingress,metrics-server,headlamp --cpus=4 --memory=8192 --extra-config=kubelet.max-pods=100
    ;;
  down)
    echo "🛑 Stopping Minikube..."
    minikube stop
    minikube delete
    exit 0
    ;;
  reset)
    echo "♻️ Resetting Minikube cluster..."
    minikube stop
    minikube delete
    minikube start --driver=docker --addons=ingress,metrics-server,headlamp --cpus=4 --memory=8192 --extra-config=kubelet.max-pods=100
    ;;
  *)
    usage
    exit 1
    ;;
esac

# -----------------------
# Create namespace if not exists
# -----------------------
kubectl get namespace $NAMESPACE >/dev/null 2>&1 || kubectl create namespace $NAMESPACE

# -----------------------
# Helper: Apply YAMLs recursively
# -----------------------
apply_yaml_recursively() {
  local dir="$1"
  echo "Applying YAMLs under $dir..."
  find "$dir" -type f -name '*.yaml' | while read -r yaml; do
    echo "Applying $yaml..."
    kubectl apply -f "$yaml" -n $NAMESPACE
  done
}

# -----------------------
# Wait for Ready Pod
# -----------------------
wait_for_ready() {
  local label="$1"
  local name="$2"
  echo "⏳ Waiting for $name pod..."
  until kubectl get pods -n $NAMESPACE -l "$label" -o 'jsonpath={..status.conditions[?(@.type=="Ready")].status}' 2>/dev/null | grep -q True; do
    echo "   → $name not ready yet..."
    sleep 5
  done
  echo "✅ $name is ready."
}

# -----------------------
# Layer Deploy Functions
# -----------------------
deploy_message() {
  echo "Deploying RabbitMQ..."
  kubectl apply -f "$ROOT_DIR/MessageLayer/RabbitMQ/K8s/" -n $NAMESPACE
  wait_for_ready "app=rabbitmq" "RabbitMQ"
}

deploy_sensor() {
  echo "Deploying Sensor & Detection Layer..."
  apply_yaml_recursively "$ROOT_DIR/SensorLayer/Databases/DetectionDB/Mongo/K8s"
  apply_yaml_recursively "$ROOT_DIR/SensorLayer/Databases/DetectionCacheDB/Redis/K8s"
  apply_yaml_recursively "$ROOT_DIR/SensorLayer/Services/SensorService/K8s"
  apply_yaml_recursively "$ROOT_DIR/SensorLayer/Services/DetectionService/K8s"

  wait_for_ready "app=detection-db" "DetectionDB"
  wait_for_ready "app=detection-cache-db" "DetectionCacheDB"
}

deploy_traffic() {
  echo "Deploying Traffic Layer..."
  apply_yaml_recursively "$ROOT_DIR/TrafficLayer/Databases/TrafficAnalyticsDB/PostgreSQL/K8s"
  apply_yaml_recursively "$ROOT_DIR/TrafficLayer/Databases/TrafficLightDB/MSSQL/K8s"
  apply_yaml_recursively "$ROOT_DIR/TrafficLayer/Databases/TrafficLightCacheDB/Redis/K8s"
  apply_yaml_recursively "$ROOT_DIR/TrafficLayer/Services/IntersectionControllerService/K8s"
  apply_yaml_recursively "$ROOT_DIR/TrafficLayer/Services/TrafficLightControllerService/K8s"
  apply_yaml_recursively "$ROOT_DIR/TrafficLayer/Services/TrafficLightCoordinatorService/K8s"

  wait_for_ready "app=traffic-analytics-db" "TrafficAnalyticsDB"
  wait_for_ready "app=traffic-light-db" "TrafficLightDB"
  wait_for_ready "app=traffic-light-cache-db" "TrafficLightCacheDB"
}

deploy_user() {
  echo "Deploying User Layer..."
  apply_yaml_recursively "$ROOT_DIR/UserLayer/Databases/UserDB/MSSQL/K8s"
  apply_yaml_recursively "$ROOT_DIR/UserLayer/Databases/NotificationDB/Mongo/K8s"
  apply_yaml_recursively "$ROOT_DIR/UserLayer/Services/UserService/K8s"
  apply_yaml_recursively "$ROOT_DIR/UserLayer/Services/NotificationService/K8s"

  wait_for_ready "app=user-db" "UserDB"
  wait_for_ready "app=notification-db" "NotificationDB"
}

deploy_log() {
  echo "Deploying Log Layer..."
  apply_yaml_recursively "$ROOT_DIR/LogLayer/Databases/LogDB/Mongo/K8s"
  apply_yaml_recursively "$ROOT_DIR/LogLayer/Services/LogService/K8s"
}

# -----------------------
# Main Deployment Logic
# -----------------------
deploy_layer() {
  case $1 in
    message) deploy_message ;;
    sensor)  deploy_sensor ;;
    traffic) deploy_traffic ;;
    user)    deploy_user ;;
    log)     deploy_log ;;
    all)
      deploy_message
      deploy_sensor
      deploy_traffic
      deploy_user
      deploy_log
      ;;
    *)
      echo "Unknown layer: $1"
      exit 1
      ;;
  esac
}

# -----------------------
# Execute deployment
# -----------------------
if [[ "$ACTION" == "up" ]]; then
  if [[ -z "$LAYER" || "$LAYER" == "all" ]]; then
    deploy_layer "all"
  else
    deploy_layer "$LAYER"
  fi
fi

# -----------------------
# Ingress
# -----------------------
echo "⏳ Waiting for ingress-nginx controller..."
until [[ $(kubectl get pods -n ingress-nginx -l app.kubernetes.io/component=controller -o 'jsonpath={..status.conditions[?(@.type=="Ready")].status}') == "True" ]]; do
  echo "   → ingress-nginx still starting..."
  sleep 5
done

kubectl apply -f "$ROOT_DIR/k8s.ingress.yaml" -n $NAMESPACE

echo ""
echo "✅ Selected STLS layer(s) deployed."
echo "👉 Add to /etc/hosts if not already: $(minikube ip) uniwa-stls.local"


echo ""
echo "✅ STLS deployed successfully on Minikube."
echo "👉 Add to /etc/hosts if not already:"
echo "   $(minikube ip) uniwa-stls.local"
echo ""
echo "Test example:"
echo "   curl http://uniwa-stls.local/sensors/agiou-spyridonos/"
echo ""
echo "🌐 Access Headlamp UI:"
echo "   → Run: kubectl create token headlamp --duration 24h -n headlamp"
echo "   → Then open: $(minikube service headlamp -n headlamp --url)"
echo ""
