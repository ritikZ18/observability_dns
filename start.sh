#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

echo "Starting Postgres..."
docker compose up -d postgres

echo -n "Waiting for Postgres healthcheck"
for _ in $(seq 1 60); do
  status="$(docker inspect -f '{{.State.Health.Status}}' observability_dns_postgres 2>/dev/null || true)"
  if [[ "$status" == "healthy" ]]; then
    break
  fi
  echo -n "."
  sleep 1
done
echo

echo "Applying EF Core migrations..."
docker compose --profile migrate run --rm db-migrate

echo "Starting stack (build if needed)..."
docker compose up -d --build

cat <<'EOF'

Service URLs
  UI:                 http://localhost:3000
  API (Swagger):       http://localhost:5000/swagger
  API health:          http://localhost:5000/healthz
  API readiness:       http://localhost:5000/readyz
  Jaeger UI:           http://localhost:16686
  Prometheus UI:       http://localhost:9090
  OTel Collector:      http://localhost:8888/metrics
  Postgres:            localhost:5432  (user: observability, db: observability_dns)

Handy commands
  docker compose ps
  docker compose logs -f api
  docker compose logs -f worker
EOF

