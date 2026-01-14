# Quick Start Guide

## Accessing Services

### UI (React Dashboard)
- **URL**: http://localhost:3000
- **Status**: Running (Vite dev server)
- **Note**: UI is scaffolded but not implemented yet. You'll see a blank page or error until React components are built.

### API (ASP.NET Core)
- **URL**: http://localhost:5000
- **Health Check**: http://localhost:5000/healthz
- **Readiness**: http://localhost:5000/readyz
- **Status**: Running (but Program.cs is scaffolded, endpoints not implemented yet)

### Observability Tools
- **Jaeger UI** (Traces): http://localhost:16686
- **Prometheus** (Metrics): http://localhost:9090
- **OpenTelemetry Collector**: Running on port 4317 (internal)

### Database
- **PostgreSQL**: localhost:5432
- **Database**: observability_dns
- **User**: observability
- **Password**: observability_dev

## Checking Service Status

```bash
# View all running containers
docker compose ps

# View logs for a specific service
docker compose logs api
docker compose logs worker
docker compose logs ui

# Follow logs in real-time
docker compose logs -f api

# Check service health
curl http://localhost:5000/healthz
curl http://localhost:5000/readyz
```

## Network Diagnostics & Hops

### Check Network Connectivity

```bash
# Test API connectivity
curl -v http://localhost:5000/healthz

# Test UI connectivity
curl -v http://localhost:3000

# Test database connectivity
docker compose exec postgres psql -U observability -d observability_dns -c "SELECT version();"
```

### Check Network Hops (Traceroute)

```bash
# Install traceroute if not available
sudo apt-get install -y traceroute  # Ubuntu/Debian
# or
sudo yum install -y traceroute     # RHEL/CentOS

# Trace route to external domain
traceroute google.com
traceroute 8.8.8.8

# Trace route with specific options
traceroute -n google.com          # Don't resolve hostnames (faster)
traceroute -m 15 google.com        # Max 15 hops
traceroute -w 2 google.com         # Wait 2 seconds per hop
```

### Check DNS Resolution

```bash
# Resolve DNS
nslookup google.com
dig google.com
host google.com

# Check DNS servers
cat /etc/resolv.conf

# Test DNS from inside containers
docker compose exec api nslookup google.com
docker compose exec worker nslookup google.com
```

### Check Container Network

```bash
# List Docker networks
docker network ls

# Inspect the observability network
docker network inspect observability_dns_network

# Check container IPs
docker compose exec api hostname -i
docker compose exec worker hostname -i
docker compose exec ui hostname -i

# Test connectivity between containers
docker compose exec api ping -c 3 postgres
docker compose exec worker ping -c 3 postgres
docker compose exec api ping -c 3 worker
```

### Monitor Network Traffic

```bash
# Install tcpdump if needed
sudo apt-get install -y tcpdump

# Monitor traffic on specific port
sudo tcpdump -i any port 5000    # API traffic
sudo tcpdump -i any port 3000    # UI traffic
sudo tcpdump -i any port 5432    # Database traffic

# Monitor with more detail
sudo tcpdump -i any -A -s 0 port 5000
```

### Check Port Connectivity

```bash
# Check if ports are listening
netstat -tuln | grep -E '3000|5000|5432|16686|9090'
# or
ss -tuln | grep -E '3000|5000|5432|16686|9090'

# Test port connectivity
nc -zv localhost 3000
nc -zv localhost 5000
nc -zv localhost 5432
```

### Docker Network Diagnostics

```bash
# View container network details
docker inspect observability_dns_api | grep -A 20 "NetworkSettings"
docker inspect observability_dns_worker | grep -A 20 "NetworkSettings"

# Test DNS resolution in container
docker compose exec api getent hosts postgres
docker compose exec api getent hosts worker
docker compose exec api getent hosts api
```

## Troubleshooting

### UI Not Loading
1. Check UI logs: `docker compose logs ui`
2. Verify port 3000 is accessible: `curl http://localhost:3000`
3. Check if Vite is running: Look for "VITE ready" in logs
4. Try accessing from browser: http://localhost:3000

### API Not Responding
1. Check API logs: `docker compose logs api`
2. Verify Program.cs has actual implementation (currently scaffolded)
3. Test health endpoint: `curl http://localhost:5000/healthz`

### Worker Restarting
1. Check worker logs: `docker compose logs worker --tail 50`
2. Worker Program.cs is scaffolded - needs implementation
3. Check database connectivity: `docker compose exec worker ping postgres`

### Database Connection Issues
1. Verify postgres is healthy: `docker compose ps postgres`
2. Test connection: `docker compose exec postgres psql -U observability -d observability_dns`
3. Check connection string in appsettings.json

## Next Steps

1. **Implement API endpoints** in `src/api/Program.cs`
2. **Implement Worker service** in `src/worker/Program.cs`
3. **Build React UI** components in `ui/src/`
4. **Add domain entities** in `src/domain/Entities/`
5. **Create probe runners** in `src/worker/Probers/`

## Useful Commands

```bash
# Restart all services
docker compose restart

# Restart specific service
docker compose restart api

# Stop all services
docker compose down

# Stop and remove volumes
docker compose down -v

# Rebuild and restart
docker compose up -d --build

# View resource usage
docker stats

# Clean up
docker compose down
docker system prune -a  # Careful: removes all unused images
```
