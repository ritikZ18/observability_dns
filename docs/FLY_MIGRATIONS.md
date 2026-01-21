# Running Migrations on Fly.io

## Problem

The production Docker image only contains the .NET runtime (not SDK), so `dotnet ef` commands don't work inside the container.

## Solution: Run Migrations Locally

Connect to Fly.io database from your local machine and run migrations.

### Step 1: Get Database Connection String

```bash
# Get the DATABASE_URL from Fly.io
fly secrets list --app observability-dns-api | grep DATABASE_URL

# Or get it from the attached database
fly postgres connect --app observability-dns-db
```

### Step 2: Set Local Connection String

```bash
# Export the connection string (replace with your actual DATABASE_URL)
export ConnectionStrings__DefaultConnection="postgres://observability_dns_api:YOUR_PASSWORD@observability-dns-db.flycast:5432/observability_dns_api?sslmode=disable"
```

### Step 3: Run Migrations Locally

```bash
cd /home/swamizero/observability_dns

# Make sure you have .NET SDK installed locally
dotnet --version

# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Run migrations (pointing to Fly.io database)
dotnet ef database update --project src/domain --startup-project src/api
```

### Step 4: Verify Migration

```bash
# Check API health
curl https://observability-dns-api.fly.dev/healthz

# Should return healthy status
```

---

## Alternative: Use Fly Proxy

### Step 1: Create Proxy to Database

```bash
# In one terminal, create proxy
fly proxy 5432 -a observability-dns-db
```

### Step 2: In Another Terminal, Run Migrations

```bash
# Set connection string to use localhost (via proxy)
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=observability_dns_api;Username=observability_dns_api;Password=YOUR_PASSWORD"

# Run migrations
cd /home/swamizero/observability_dns
dotnet ef database update --project src/domain --startup-project src/api
```

---

## Quick Fix Script

The migration script is located at `scripts/deployment/run-fly-migrations.sh`:

```bash
#!/bin/bash

echo "🔗 Getting database connection string..."
DB_URL=$(fly secrets list --app observability-dns-api | grep DATABASE_URL | awk '{print $2}')

if [ -z "$DB_URL" ]; then
    echo "❌ Could not find DATABASE_URL"
    echo "Run: fly secrets list --app observability-dns-api"
    exit 1
fi

echo "✅ Found database URL"
echo "📦 Running migrations..."

export ConnectionStrings__DefaultConnection="$DB_URL"

cd /home/swamizero/observability_dns
dotnet ef database update --project src/domain --startup-project src/api

if [ $? -eq 0 ]; then
    echo "✅ Migrations completed successfully!"
    echo "🔍 Verifying API health..."
    curl https://observability-dns-api.fly.dev/healthz
else
    echo "❌ Migration failed"
    exit 1
fi
```

Make it executable and run:
```bash
chmod +x scripts/deployment/run-fly-migrations.sh
./scripts/deployment/run-fly-migrations.sh
```
