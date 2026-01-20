# Fly.io Deployment Guide - Free Tier

Complete step-by-step guide to deploy DNS & TLS Observatory to Fly.io for free.

## Prerequisites

1. **Fly.io Account**: Sign up at https://fly.io (free tier available)
2. **Fly CLI**: Install Fly.io CLI
3. **GitHub Account**: (optional, for CI/CD)

---

## Step 1: Install Fly CLI

### macOS
```bash
curl -L https://fly.io/install.sh | sh
```

### Linux/WSL
```bash
curl -L https://fly.io/install.sh | sh
```

### Windows
```powershell
powershell -Command "iwr https://fly.io/install.ps1 -useb | iex"
```

### Verify Installation
```bash
fly version
```

---

## Step 2: Login to Fly.io

```bash
fly auth login
```

This will open your browser to authenticate.

---

## Step 3: Create PostgreSQL Database

Fly.io offers managed PostgreSQL (free tier available):

```bash
# Create PostgreSQL database
fly postgres create --name observability-dns-db --region ams --vm-size shared-cpu-1x --volume-size 1

# Wait for database to be ready (takes 2-3 minutes)
fly status --app observability-dns-db

# Get connection string
fly postgres connect --app observability-dns-db
```

**Note:** The free tier includes:
- 1GB storage
- Shared CPU
- Perfect for development/testing

---

## Step 4: Create API App

```bash
# Navigate to project root
cd /home/swamizero/observability_dns

# Create API app (use existing fly.toml)
fly apps create observability-dns-api

# Set secrets (database connection will be set automatically)
fly secrets set \
  ASPNETCORE_ENVIRONMENT=Production \
  ASPNETCORE_URLS=http://+:8080 \
  --app observability-dns-api

# Attach PostgreSQL to API app
fly postgres attach --app observability-dns-api observability-dns-db

# This automatically sets: ConnectionStrings__DefaultConnection
```

---

## Step 5: Deploy API

```bash
# Deploy API (this will build and deploy)
fly deploy --config fly.toml --app observability-dns-api

# Check status
fly status --app observability-dns-api

# View logs
fly logs --app observability-dns-api

# Get URL
fly status --app observability-dns-api | grep "Hostname"
```

**Your API will be available at:** `https://observability-dns-api.fly.dev`

---

## Step 6: Run Database Migrations

After API is deployed, run migrations:

```bash
# Option 1: Run migration via API container
fly ssh console --app observability-dns-api

# Inside the container:
cd /app
dotnet ef database update --project ../domain --startup-project .

# Exit container
exit
```

**OR** Create a migration job:

```bash
# Create a one-off migration job
fly ssh console --app observability-dns-api -C "cd /app && dotnet ef database update --project ../domain --startup-project ."
```

---

## Step 7: Create Worker App

```bash
# Create worker app
fly apps create observability-dns-worker

# Attach PostgreSQL
fly postgres attach --app observability-dns-worker observability-dns-db

# Set secrets
fly secrets set \
  ASPNETCORE_ENVIRONMENT=Production \
  --app observability-dns-worker

# Deploy worker
fly deploy --config fly.worker.toml --app observability-dns-worker

# Check status
fly status --app observability-dns-worker
```

---

## Step 8: Create UI App

```bash
# Create UI app
fly apps create observability-dns-ui

# Set environment variable for API URL
fly secrets set \
  VITE_API_URL=https://observability-dns-api.fly.dev \
  --app observability-dns-ui

# Deploy UI
fly deploy --config fly.ui.toml --app observability-dns-ui

# Check status
fly status --app observability-dns-ui
```

**Your UI will be available at:** `https://observability-dns-ui.fly.dev`

---

## Step 9: Update UI to Use API URL

The UI needs to know the API URL. Update the UI configuration:

```bash
# Set API URL in UI secrets
fly secrets set \
  VITE_API_URL=https://observability-dns-api.fly.dev \
  --app observability-dns-ui

# Redeploy UI
fly deploy --config fly.ui.toml --app observability-dns-ui
```

**OR** Update `ui/src/services/api.ts` to use environment variable:

```typescript
const API_BASE_URL = (import.meta as any).env?.VITE_API_URL || 'https://observability-dns-api.fly.dev';
```

---

## Step 10: Verify Deployment

### Check API Health
```bash
curl https://observability-dns-api.fly.dev/healthz
curl https://observability-dns-api.fly.dev/readyz
```

### Check All Services
```bash
# API
fly status --app observability-dns-api

# Worker
fly status --app observability-dns-worker

# UI
fly status --app observability-dns-ui

# Database
fly status --app observability-dns-db
```

### View Logs
```bash
# API logs
fly logs --app observability-dns-api

# Worker logs
fly logs --app observability-dns-worker

# UI logs
fly logs --app observability-dns-ui
```

---

## Configuration Summary

### Apps Created:
1. **observability-dns-api** - API service
2. **observability-dns-worker** - Background worker
3. **observability-dns-ui** - Frontend UI
4. **observability-dns-db** - PostgreSQL database

### URLs:
- **API**: `https://observability-dns-api.fly.dev`
- **UI**: `https://observability-dns-ui.fly.dev`
- **Health Check**: `https://observability-dns-api.fly.dev/healthz`

---

## Environment Variables

### API App
```bash
fly secrets list --app observability-dns-api
```

Should show:
- `ConnectionStrings__DefaultConnection` (auto-set by PostgreSQL attachment)
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`

### Worker App
```bash
fly secrets list --app observability-dns-worker
```

Should show:
- `ConnectionStrings__DefaultConnection` (auto-set by PostgreSQL attachment)
- `ASPNETCORE_ENVIRONMENT=Production`

### UI App
```bash
fly secrets list --app observability-dns-ui
```

Should show:
- `VITE_API_URL=https://observability-dns-api.fly.dev`

---

## Troubleshooting

### Issue: "Could not find a Dockerfile"

**Solution:** Make sure you're in the project root and specify the config:
```bash
fly deploy --config fly.toml --app observability-dns-api
```

### Issue: Database Connection Failed

**Solution:** 
1. Verify PostgreSQL is attached:
   ```bash
   fly postgres list
   fly postgres attach --app observability-dns-api observability-dns-db
   ```

2. Check connection string:
   ```bash
   fly secrets list --app observability-dns-api | grep ConnectionStrings
   ```

### Issue: Migration Failed

**Solution:**
1. SSH into API container:
   ```bash
   fly ssh console --app observability-dns-api
   ```

2. Check if EF Core tools are available:
   ```bash
   dotnet ef --version
   ```

3. If not, install:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

4. Run migration:
   ```bash
   cd /app
   dotnet ef database update --project ../domain --startup-project .
   ```

### Issue: UI Can't Connect to API

**Solution:**
1. Check CORS settings in API
2. Verify API URL in UI secrets:
   ```bash
   fly secrets list --app observability-dns-ui
   ```

3. Update `ui/src/services/api.ts`:
   ```typescript
   const API_BASE_URL = 'https://observability-dns-api.fly.dev';
   ```

### Issue: Worker Not Running

**Solution:**
1. Check worker logs:
   ```bash
   fly logs --app observability-dns-worker
   ```

2. Verify worker is running:
   ```bash
   fly status --app observability-dns-worker
   ```

3. Restart worker:
   ```bash
   fly apps restart observability-dns-worker
   ```

---

## Free Tier Limits

### Fly.io Free Tier:
- **3 shared-cpu-1x VMs** (256MB RAM each)
- **3GB total storage**
- **160GB outbound data transfer/month**
- **Apps sleep after 5 minutes of inactivity** (free tier)

### PostgreSQL Free Tier:
- **1GB storage**
- **Shared CPU**
- **Perfect for development**

### Cost:
- **$0/month** if you stay within free tier limits
- **~$5-10/month** if you need more resources

---

## Scaling (When Needed)

### Scale API
```bash
fly scale count 2 --app observability-dns-api
```

### Scale Worker
```bash
fly scale count 1 --app observability-dns-worker
```

### Upgrade Database
```bash
fly postgres upgrade --app observability-dns-db
```

---

## Monitoring

### View Metrics
```bash
# API metrics
fly metrics --app observability-dns-api

# Worker metrics
fly metrics --app observability-dns-worker
```

### View Logs
```bash
# Real-time logs
fly logs --app observability-dns-api

# Follow logs
fly logs --app observability-dns-api -f
```

---

## Quick Commands Reference

```bash
# List all apps
fly apps list

# Check app status
fly status --app observability-dns-api

# View logs
fly logs --app observability-dns-api

# SSH into container
fly ssh console --app observability-dns-api

# Restart app
fly apps restart observability-dns-api

# Scale app
fly scale count 2 --app observability-dns-api

# View secrets
fly secrets list --app observability-dns-api

# Set secret
fly secrets set KEY=value --app observability-dns-api

# Deploy
fly deploy --config fly.toml --app observability-dns-api
```

---

## Complete Deployment Script

Save this as `deploy-fly.sh`:

```bash
#!/bin/bash

set -e

echo "🚀 Deploying DNS & TLS Observatory to Fly.io"

# Step 1: Create database
echo "📦 Creating PostgreSQL database..."
fly postgres create --name observability-dns-db --region ams --vm-size shared-cpu-1x --volume-size 1

# Wait for database
echo "⏳ Waiting for database to be ready..."
sleep 30

# Step 2: Create and deploy API
echo "🔧 Creating API app..."
fly apps create observability-dns-api || echo "API app already exists"

echo "🔗 Attaching database to API..."
fly postgres attach --app observability-dns-api observability-dns-db || echo "Already attached"

echo "🚀 Deploying API..."
fly deploy --config fly.toml --app observability-dns-api

# Step 3: Create and deploy Worker
echo "🔧 Creating Worker app..."
fly apps create observability-dns-worker || echo "Worker app already exists"

echo "🔗 Attaching database to Worker..."
fly postgres attach --app observability-dns-worker observability-dns-db || echo "Already attached"

echo "🚀 Deploying Worker..."
fly deploy --config fly.worker.toml --app observability-dns-worker

# Step 4: Create and deploy UI
echo "🔧 Creating UI app..."
fly apps create observability-dns-ui || echo "UI app already exists"

echo "🔧 Setting API URL..."
fly secrets set VITE_API_URL=https://observability-dns-api.fly.dev --app observability-dns-ui

echo "🚀 Deploying UI..."
fly deploy --config fly.ui.toml --app observability-dns-ui

echo "✅ Deployment complete!"
echo ""
echo "📋 Your services:"
echo "  API: https://observability-dns-api.fly.dev"
echo "  UI: https://observability-dns-ui.fly.dev"
echo ""
echo "📝 Next steps:"
echo "  1. Run migrations: fly ssh console --app observability-dns-api"
echo "  2. Check health: curl https://observability-dns-api.fly.dev/healthz"
echo "  3. Open UI: https://observability-dns-ui.fly.dev"
```

Make it executable:
```bash
chmod +x deploy-fly.sh
./deploy-fly.sh
```

---

## Post-Deployment Checklist

- [ ] API deployed and healthy
- [ ] Worker deployed and running
- [ ] UI deployed and accessible
- [ ] Database migrations run
- [ ] API health check passes
- [ ] UI can connect to API
- [ ] Worker is executing probes
- [ ] Logs are visible

---

## Support

- **Fly.io Docs**: https://fly.io/docs
- **Fly.io Community**: https://community.fly.io
- **Fly.io Status**: https://status.fly.io

---

**Good luck with your deployment! 🚀**
