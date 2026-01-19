# Manual Worker Guide

The manual worker is an alternative execution mode that runs probe jobs continuously, checking for enabled domains and executing probes at regular intervals. This is useful for testing, development, or scenarios where you don't want to use Quartz scheduling.

## Overview

- **Standard Worker**: Uses Quartz scheduler to run probes at domain-specific intervals (e.g., every 5 minutes)
- **Manual Worker**: Continuously polls and executes all enabled probes every N seconds (default: 30)

## Use Cases

- **Testing**: Immediately test probe execution without waiting for scheduled intervals
- **Development**: Faster feedback loop when developing probe runners
- **Debugging**: See probe results in real-time
- **Quick Checks**: Run a batch of probes on-demand

## Usage

### Option 1: Shell Script (Easiest)

The shell script is the simplest way to run probes manually.

```bash
# Make executable
chmod +x scripts/manual-worker.sh

# Set environment variables
export DB_HOST=localhost
export DB_PORT=5432
export DB_NAME=observability_dns
export DB_USER=observability
export DB_PASSWORD=observability_dev
export CHECK_INTERVAL=30

# Run
./scripts/manual-worker.sh
```

**Features**:
- Checks database for enabled domains
- Executes all enabled checks
- Waits N seconds before next iteration
- Stops on Ctrl+C or container stop signal
- Color-coded output

**Environment Variables**:
- `DB_HOST`: Database host (default: `localhost` or `postgres` if in Docker)
- `DB_PORT`: Database port (default: `5432`)
- `DB_NAME`: Database name (default: `observability_dns`)
- `DB_USER`: Database user (default: `observability`)
- `DB_PASSWORD`: Database password (default: `observability_dev`)
- `CHECK_INTERVAL`: Seconds between iterations (default: `30`)

### Option 2: .NET Manual Worker

Use the C# `ManualWorker` class for more control and better integration.

#### Setup

1. **Modify `Program.cs`** to use manual worker:

```csharp
// In src/worker/Program.cs
if (Environment.GetEnvironmentVariable("USE_MANUAL_WORKER") == "true")
{
    builder.Services.AddHostedService<ManualWorker>();
}
else
{
    builder.Services.AddHostedService<ProbeScheduler>();
}
```

2. **Run with manual worker**:

```bash
cd src/worker

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=observability_dns;Username=observability;Password=observability_dev"
export USE_MANUAL_WORKER=true
export MANUAL_WORKER_INTERVAL=30

dotnet run
```

Or pass command-line argument:
```bash
dotnet run --use-manual-worker
```

### Option 3: Docker Compose

Add to `docker-compose.yml`:

```yaml
manual-worker:
  build:
    context: .
    dockerfile: src/worker/Dockerfile
  container_name: observability_dns_manual_worker
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=observability_dns;Username=observability;Password=observability_dev
    - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
    - USE_MANUAL_WORKER=true
    - MANUAL_WORKER_INTERVAL=30
  depends_on:
    postgres:
      condition: service_healthy
  restart: unless-stopped
```

Then run:
```bash
docker compose up manual-worker
```

## How It Works

1. **Initialization**: Worker starts and loads configuration
2. **Polling Loop**: Every N seconds (configured interval):
   - Queries database for all enabled domains with enabled checks
   - For each domain/check combination:
     - Executes appropriate probe runner (DNS, TLS, or HTTP)
     - Saves probe run result to database
     - Logs execution result
3. **Continuous Execution**: Repeats until stopped
4. **Clean Shutdown**: Handles Ctrl+C or Docker stop gracefully

## Comparison: Scheduled vs Manual Worker

| Feature | Scheduled Worker | Manual Worker |
|---------|-----------------|---------------|
| **Execution** | Domain-specific intervals | Fixed polling interval |
| **Frequency** | Per-domain (1, 5, 15 min) | Global (every 30s default) |
| **Efficiency** | More efficient (only runs when needed) | Less efficient (always polling) |
| **Testing** | Slow (wait for schedule) | Fast (immediate execution) |
| **Use Case** | Production | Development/Testing |

## Examples

### Run Once and Stop

```bash
# Set very short interval, run once manually
export CHECK_INTERVAL=1
./scripts/manual-worker.sh &
WORKER_PID=$!

# Wait for one iteration
sleep 5

# Stop worker
kill $WORKER_PID
```

### Monitor in Real-Time

```bash
# Run manual worker
./scripts/manual-worker.sh

# In another terminal, watch probe runs
watch -n 2 'curl -s "http://localhost:5000/api/probe-runs?limit=5" | jq'
```

### Test Specific Domain

```bash
# Enable only one domain temporarily
docker compose exec postgres psql -U observability -d observability_dns \
  -c "UPDATE domains SET enabled = false WHERE name != 'google.com';"

# Run manual worker
./scripts/manual-worker.sh

# Re-enable all domains
docker compose exec postgres psql -U observability -d observability_dns \
  -c "UPDATE domains SET enabled = true;"
```

## Troubleshooting

### Worker Not Finding Domains

Check if domains are enabled:
```bash
docker compose exec postgres psql -U observability -d observability_dns \
  -c "SELECT name, enabled FROM domains;"
```

### Worker Not Executing Probes

Check worker logs:
```bash
docker compose logs manual-worker -f
```

Verify probe runners are registered:
```bash
docker compose exec manual-worker dotnet ObservabilityDns.Worker.dll --list-probes
```

### Database Connection Issues

Test connection:
```bash
docker compose exec postgres psql -U observability -d observability_dns -c "SELECT 1;"
```

Verify connection string:
```bash
echo $ConnectionStrings__DefaultConnection
```

## Stopping the Worker

- **Shell Script**: Press `Ctrl+C`
- **Docker**: `docker compose stop manual-worker` or `docker stop observability_dns_manual_worker`
- **Systemd**: `systemctl stop observability-dns-manual-worker`

The worker handles graceful shutdown and will:
- Complete current probe execution
- Save any pending results
- Clean up resources
- Exit cleanly

## Best Practices

1. **Development**: Use manual worker for faster iteration
2. **Testing**: Use manual worker to verify probe functionality
3. **Production**: Use scheduled worker for efficient resource usage
4. **Debugging**: Use manual worker with short intervals to see immediate results

## Performance Considerations

- **Interval**: Shorter intervals = more frequent execution = higher CPU/database usage
- **Concurrency**: Manual worker runs probes sequentially (one at a time)
- **Database Load**: Each iteration queries database for enabled domains
- **Recommendation**: Use 30+ second intervals for production-like testing

## Integration with CI/CD

```yaml
# Example GitHub Actions workflow
- name: Test Probes
  run: |
    docker compose up -d postgres api
    export DB_HOST=localhost
    export CHECK_INTERVAL=5
    timeout 60 ./scripts/manual-worker.sh || true
    # Verify probe runs were created
    curl -f http://localhost:5000/api/probe-runs?limit=1
```
