# Quick Start Guide

Get up and running with DNS & TLS Observatory quickly.

## Prerequisites

- Docker and Docker Compose
- .NET 8 SDK (for local development)
- Node.js 18+ (for UI development)

## Local Development

### 1. Start Services

```bash
docker compose up -d
```

### 2. Run Migrations

```bash
docker compose --profile migrate up db-migrate
```

### 3. Access Services

- **UI**: http://localhost:3000
- **API**: http://localhost:5000
- **API Docs**: http://localhost:5000/swagger
- **Health**: http://localhost:5000/healthz

## Deployment

### Fly.io Deployment

```bash
./scripts/deployment/deploy-fly.sh
```

### Run Migrations on Fly.io

```bash
./scripts/deployment/run-fly-migrations.sh
```

### Fix UI Issues on Fly.io

```bash
./scripts/deployment/fix-fly-ui.sh
```

## Testing

### Run All Tests

```bash
# Backend tests
dotnet test

# E2E tests
cd tests/e2e && npm test

# Smoke tests
./tests/smoke/run-smoke-tests.sh local
```

## Next Steps

- [Setup Guide](./SETUP.md) - Detailed setup instructions
- [Testing Guide](./TESTING.md) - Testing documentation
- [Deployment Guide](./DEPLOYMENT.md) - Deployment options
