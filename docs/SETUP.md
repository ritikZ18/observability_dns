# Setup Guide

This guide walks you through setting up the DNS & TLS Observatory for local development.

## Prerequisites

- **Docker** and **Docker Compose** (latest version)
- **.NET 8 SDK** (for local development without Docker)
- **Node.js 18+** (for local UI development)
- **Git**

## Quick Start (Docker)

The fastest way to get started is using Docker Compose:

```bash
# Clone the repository
git clone <repository-url>
cd observability_dns

# Start all services
docker compose up -d

# Check service status
docker compose ps

# View logs
docker compose logs -f
```

Services will be available at:
- **UI**: http://localhost:3000
- **API**: http://localhost:5000
- **Jaeger UI**: http://localhost:16686
- **Prometheus**: http://localhost:9090

## Manual Setup (Local Development)

### 1. Database Setup

Start PostgreSQL:

```bash
docker run -d \
  --name observability_dns_postgres \
  -e POSTGRES_USER=observability \
  -e POSTGRES_PASSWORD=observability_dev \
  -e POSTGRES_DB=observability_dns \
  -p 5432:5432 \
  -v $(pwd)/infra/database/init.sql:/docker-entrypoint-initdb.d/init.sql \
  postgres:15-alpine
```

Initialize schema:

```bash
psql -h localhost -U observability -d observability_dns -f infra/database/schema.sql
```

### 2. API Setup

```bash
cd src/api

# Restore dependencies
dotnet restore

# Set environment variables
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=observability_dns;Username=observability;Password=observability_dev"
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"

# Run migrations (when implemented)
dotnet ef database update --project ../domain --startup-project .

# Run API
dotnet run
```

API will be available at: http://localhost:5000

### 3. Worker Setup

```bash
cd src/worker

# Restore dependencies
dotnet restore

# Set environment variables
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=observability_dns;Username=observability;Password=observability_dev"
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"

# Run worker
dotnet run
```

### 4. UI Setup

```bash
cd ui

# Install dependencies
npm install

# Set API URL (if not using proxy)
export VITE_API_URL=http://localhost:5000

# Run dev server
npm run dev
```

UI will be available at: http://localhost:3000

### 5. Observability Stack (Optional)

Start OpenTelemetry Collector, Jaeger, and Prometheus:

```bash
# Using Docker Compose (recommended)
docker compose up -d otel-collector jaeger prometheus

# Or manually:
docker run -d -p 4317:4317 -p 4318:4318 \
  -v $(pwd)/infra/otel-collector-config.yaml:/etc/otel-collector-config.yaml \
  otel/opentelemetry-collector:latest \
  --config=/etc/otel-collector-config.yaml

docker run -d -p 16686:16686 jaegertracing/all-in-one:latest

docker run -d -p 9090:9090 \
  -v $(pwd)/infra/prometheus.yml:/etc/prometheus/prometheus.yml \
  prom/prometheus:latest
```

## Environment Variables

Create a `.env` file in the root directory:

```bash
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=observability_dns;Username=observability;Password=observability_dev

# OpenTelemetry
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317

# Slack (optional)
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/YOUR/WEBHOOK/URL

# SMTP (optional)
SMTP__HOST=smtp.gmail.com
SMTP__PORT=587
SMTP__USERNAME=your-email@gmail.com
SMTP__PASSWORD=your-app-password
SMTP__FROMEMAIL=your-email@gmail.com
```

## Database Migrations

### Using EF Core (Recommended)

```bash
# Create migration
dotnet ef migrations add MigrationName --project src/domain --startup-project src/api

# Apply migration
dotnet ef database update --project src/domain --startup-project src/api

# Using Docker
docker compose --profile migrate up db-migrate
```

### Using SQL Scripts

```bash
psql -h localhost -U observability -d observability_dns -f infra/database/schema.sql
```

## Troubleshooting

### Database Connection Issues

1. Verify PostgreSQL is running:
   ```bash
   docker ps | grep postgres
   ```

2. Test connection:
   ```bash
   psql -h localhost -U observability -d observability_dns
   ```

3. Check connection string in environment variables

### Port Conflicts

If ports are already in use:

1. Change ports in `docker-compose.yml`
2. Or stop conflicting services:
   ```bash
   # Find process using port 5000
   lsof -i :5000
   # Kill process
   kill -9 <PID>
   ```

### Build Errors

1. Clear build artifacts:
   ```bash
   dotnet clean
   rm -rf bin/ obj/
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

### UI Not Connecting to API

1. Check API is running: `curl http://localhost:5000/healthz`
2. Verify proxy configuration in `vite.config.ts`
3. Check CORS settings in API

## Next Steps

1. Add your first domain to monitor
2. Configure alert rules
3. Set up Slack/Email notifications
4. Explore Jaeger traces and Prometheus metrics

## Development Workflow

1. Make code changes
2. Rebuild containers: `docker compose build`
3. Restart services: `docker compose up -d`
4. Check logs: `docker compose logs -f <service-name>`

## Testing

```bash
# Run API tests
cd src/api
dotnet test

# Run worker tests
cd src/worker
dotnet test

# Run all tests
dotnet test
```
