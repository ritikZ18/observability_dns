# Fly.io Deployment Fix Guide

## Issues Found

1. **Database Connection**: Fly.io sets `DATABASE_URL` but .NET app expects `ConnectionStrings__DefaultConnection`
2. **Health Check Failing**: Database health check fails because connection string is wrong
3. **Trial Machine Stopping**: Free trial machines stop after 5 minutes (need credit card for longer runs)

## Fixes Applied

### 1. Connection String Mapping

Updated `src/api/Program.cs` to read `DATABASE_URL` first, then fall back to `ConnectionStrings__DefaultConnection`:

```csharp
// Support both DATABASE_URL (Fly.io) and ConnectionStrings__DefaultConnection
var connectionString = builder.Configuration["DATABASE_URL"] 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ObservabilityDnsDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### 2. Fly.toml Configuration

Updated `fly.toml`:
- `min_machines_running = 1` (was 0) - keeps machine running
- `auto_stop_machines = false` (was true) - prevents auto-stop
- Increased health check grace period to 10s

### 3. Health Check Configuration

The health check was failing because:
- Database connection string was wrong
- Health check ran too early (before migrations)

**Solution**: Health check will pass after connection string is fixed and migrations are run.

## Deployment Steps

### Step 1: Rebuild and Redeploy API

```bash
cd /home/swamizero/observability_dns

# Rebuild and deploy
fly deploy --config fly.toml --app observability-dns-api
```

### Step 2: Run Database Migrations

After deployment succeeds:

```bash
# SSH into the container
fly ssh console --app observability-dns-api

# Inside container, run migrations
cd /app
dotnet ef database update --project ../domain --startup-project .

# Exit
exit
```

**Note**: EF Core tools might not be installed in the container. If migration fails:

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Then run migration
dotnet ef database update --project ../domain --startup-project .
```

### Step 3: Verify Health Check

```bash
# Check health
curl https://observability-dns-api.fly.dev/healthz

# Should return:
# {"status":"Healthy","checks":[...]}
```

### Step 4: Check Logs

```bash
fly logs --app observability-dns-api
```

## Alternative: Set Connection String Manually

If the automatic mapping doesn't work, set it manually:

```bash
# Get DATABASE_URL
fly secrets list --app observability-dns-api | grep DATABASE_URL

# Set ConnectionStrings__DefaultConnection
fly secrets set ConnectionStrings__DefaultConnection="<DATABASE_URL_VALUE>" --app observability-dns-api
```

## Trial Account Limitation

**Important**: Free trial machines stop after 5 minutes without a credit card.

To keep machines running:
1. Add a credit card at https://fly.io/trial
2. Or accept that machines will auto-stop (they'll restart on first request)

The `min_machines_running = 1` setting helps, but trial accounts still have limitations.

## Troubleshooting

### Issue: Health check still failing

**Check connection string:**
```bash
fly ssh console --app observability-dns-api
env | grep DATABASE
```

**Verify database is accessible:**
```bash
fly postgres connect --app observability-dns-db
```

### Issue: Migration fails

**EF Core tools not installed:**
```bash
fly ssh console --app observability-dns-api
dotnet tool install --global dotnet-ef
export PATH="$PATH:/root/.dotnet/tools"
dotnet ef database update --project ../domain --startup-project .
```

### Issue: Machine keeps stopping

**Trial account limitation:**
- Add credit card: https://fly.io/trial
- Or accept auto-stop (machines restart on first request)

## Next Steps

After API is working:

1. **Deploy Worker:**
   ```bash
   fly deploy --config fly.worker.toml --app observability-dns-worker
   ```

2. **Deploy UI:**
   ```bash
   fly secrets set VITE_API_URL=https://observability-dns-api.fly.dev --app observability-dns-ui
   fly deploy --config fly.ui.toml --app observability-dns-ui
   ```

3. **Verify Everything:**
   ```bash
   curl https://observability-dns-api.fly.dev/healthz
   curl https://observability-dns-api.fly.dev/api/domains
   ```

---

**Status**: Connection string fix applied. Ready to redeploy!
