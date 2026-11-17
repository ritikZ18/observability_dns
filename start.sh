#!/usr/bin/env bash
set -Eeuo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_DIR"

usage() {
  cat <<'EOF'
Observability DNS local stack

Usage:
  ./start.sh                         Start the full stack (build + migrate)
  ./start.sh --start [OPTIONS]       Start the full stack
  ./start.sh --stop                  Stop containers; preserve database data
  ./start.sh --restart [OPTIONS]     Recreate the full stack
  ./start.sh --status                Show container status and local URLs
  ./start.sh --logs [SERVICE]        Follow all logs, or one service's logs
  ./start.sh --migrate               Run database migrations only
  ./start.sh --build                 Build all local images only
  ./start.sh --urls                  Print local service URLs
  ./start.sh --help                  Show this help

Start/restart options:
  --no-build                         Reuse existing images
  --skip-migrate                     Do not run database migrations

Services for --logs:
  api, worker, ui, postgres, otel-collector, jaeger, prometheus

Examples:
  ./start.sh
  ./start.sh --start --no-build
  ./start.sh --restart --skip-migrate
  ./start.sh --logs api

Database data is stored in a named Docker volume and is preserved by stop and
restart. Use "docker compose down -v" manually only when you intend to erase it.
EOF
}

compose() { docker compose "$@"; }

require_docker() {
  command -v docker >/dev/null 2>&1 || {
    echo "Docker is not installed or is not in PATH." >&2
    exit 1
  }
  docker compose version >/dev/null 2>&1 || {
    echo "Docker Compose is unavailable. Install the Docker Compose plugin." >&2
    exit 1
  }
  docker info >/dev/null 2>&1 || {
    echo "Docker is not running or this user cannot access the Docker daemon." >&2
    echo "Start Docker, then try again." >&2
    exit 1
  }
}

show_urls() {
  local ui_port="${UI_PORT:-3002}"
  local prometheus_port="${PROMETHEUS_PORT:-9091}"
  cat <<EOF

Local service URLs
  UI dashboard:        http://localhost:${ui_port}
  API / Swagger:       http://localhost:5000/swagger
  API health:          http://localhost:5000/healthz
  API readiness:       http://localhost:5000/readyz
  Jaeger traces:       http://localhost:16686
  Prometheus metrics:  http://localhost:${prometheus_port}
  OTel metrics:        http://localhost:8888/metrics
  PostgreSQL:          localhost:5432 (observability / observability_dns)
EOF
}

run_migrations() {
  echo "Applying database migrations..."
  compose --profile migrate run --rm db-migrate
}

start_stack() {
  local build="$1" migrate="$2"

  echo "Starting PostgreSQL..."
  compose up -d --wait --wait-timeout 90 postgres

  if [[ "$migrate" == true ]]; then
    run_migrations
  fi

  echo "Starting the application and observability services..."
  local up_args=(up -d --wait --wait-timeout 120)
  if [[ "$build" == true ]]; then
    up_args+=(--build)
  fi
  compose "${up_args[@]}"

  echo
  compose ps
  show_urls
  echo
  echo "Follow logs with: ./start.sh --logs"
}

action="start"
build=true
migrate=true
log_service=""

case "${1:-}" in
  ""|--start|start)
    [[ $# -eq 0 ]] || shift
    ;;
  --restart|restart) action="restart"; shift ;;
  --stop|stop) action="stop"; shift ;;
  --status|status) action="status"; shift ;;
  --logs|logs)
    action="logs"
    shift
    if [[ $# -gt 0 && "${1:-}" != --* ]]; then
      log_service="$1"
      shift
    fi
    ;;
  --migrate|migrate) action="migrate-only"; shift ;;
  --build|build) action="build-only"; shift ;;
  --urls|urls) action="urls"; shift ;;
  --help|-h|help) usage; exit 0 ;;
  *) echo "Unknown command: $1" >&2; usage >&2; exit 2 ;;
esac

if [[ "$action" == "start" || "$action" == "restart" ]]; then
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --no-build) build=false ;;
      --skip-migrate) migrate=false ;;
      --help|-h) usage; exit 0 ;;
      *) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
    esac
    shift
  done
elif [[ $# -gt 0 ]]; then
  echo "Unexpected argument: $1" >&2
  usage >&2
  exit 2
fi

if [[ "$action" == "urls" ]]; then
  show_urls
  exit 0
fi

require_docker

case "$action" in
  start) start_stack "$build" "$migrate" ;;
  restart)
    echo "Recreating containers (database data is preserved)..."
    compose down
    start_stack "$build" "$migrate"
    ;;
  stop)
    compose stop
    echo "Stopped the stack. Database data was preserved."
    ;;
  status)
    compose ps -a
    show_urls
    ;;
  logs)
    if [[ -n "$log_service" ]]; then
      compose logs -f --tail=200 "$log_service"
    else
      compose logs -f --tail=200
    fi
    ;;
  migrate-only)
    compose up -d --wait --wait-timeout 90 postgres
    run_migrations
    ;;
  build-only) compose --profile migrate build ;;
esac
