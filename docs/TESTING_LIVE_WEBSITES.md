# Testing with Live Websites

This guide explains how to test your DNS & TLS Observatory by monitoring real websites.

## Overview

Once your API and Worker are implemented, you'll be able to:
1. Add domains to monitor via the API
2. Worker automatically probes them on schedule (1/5/15 min intervals)
3. View results in the UI dashboard
4. Receive alerts when issues are detected

## Test Domains to Monitor

### Recommended Test Set

#### 1. Reliable Public Services (Baseline)
These should always be healthy:
- `google.com` - Fast, reliable DNS and HTTP
- `github.com` - Good for testing HTTPS/TLS
- `aws.amazon.com` - Enterprise-grade infrastructure
- `microsoft.com` - Large-scale DNS setup

#### 2. Services with Variable Latency
Good for testing alert thresholds:
- `reddit.com` - Can have variable response times
- `medium.com` - Sometimes slower responses
- `stackoverflow.com` - Good for HTTP monitoring

#### 3. TLS Certificate Testing
Use these to test certificate monitoring:
- `expired.badssl.com` - Expired certificate (will fail TLS check)
- `self-signed.badssl.com` - Self-signed cert (will fail validation)
- `wrong.host.badssl.com` - Hostname mismatch (will fail)
- `sha256.badssl.com` - Valid certificate (should pass)

**Note**: BadSSL.com provides test domains specifically for SSL/TLS testing.

#### 4. DNS Testing
- `example.com` - Standard test domain
- `test.com` - Simple domain
- `nonexistent-domain-12345.com` - Should trigger NXDOMAIN

#### 5. Your Own Endpoints
Create test endpoints you control:
- Your own API hosted on Render/Azure/AWS
- A simple health check endpoint
- A domain you can intentionally break for testing

## How to Add Domains (Once API is Implemented)

### Via API (REST)

```bash
# Add a domain to monitor
curl -X POST http://localhost:5000/api/domains \
  -H "Content-Type: application/json" \
  -d '{
    "name": "google.com",
    "intervalMinutes": 5,
    "enabled": true
  }'

# Add multiple domains
curl -X POST http://localhost:5000/api/domains \
  -H "Content-Type: application/json" \
  -d '{
    "name": "github.com",
    "intervalMinutes": 1,
    "enabled": true
  }'

curl -X POST http://localhost:5000/api/domains \
  -H "Content-Type: application/json" \
  -d '{
    "name": "expired.badssl.com",
    "intervalMinutes": 5,
    "enabled": true
  }'
```

### Via Database (Direct)

```bash
# Connect to database
docker compose exec postgres psql -U observability -d observability_dns

# Insert test domains
INSERT INTO domains (name, enabled, interval_minutes) VALUES
  ('google.com', true, 5),
  ('github.com', true, 1),
  ('expired.badssl.com', true, 5),
  ('self-signed.badssl.com', true, 5);

# Enable checks for each domain
INSERT INTO checks (domain_id, check_type, enabled)
SELECT id, 'DNS', true FROM domains WHERE name = 'google.com';

INSERT INTO checks (domain_id, check_type, enabled)
SELECT id, 'TLS', true FROM domains WHERE name = 'google.com';

INSERT INTO checks (domain_id, check_type, enabled)
SELECT id, 'HTTP', true FROM domains WHERE name = 'google.com';
```

## Testing Workflow

### Step 1: Start Services

```bash
cd /home/swamizero/observability_dns
docker compose up -d
```

### Step 2: Add Test Domains

Once API is implemented, add domains via API or database (see above).

### Step 3: Monitor Probe Execution

```bash
# Watch worker logs for probe execution
docker compose logs -f worker

# You should see logs like:
# [INFO] Running DNS probe for google.com
# [INFO] DNS probe completed: Success, 45ms
# [INFO] Running TLS probe for google.com
# [INFO] TLS probe completed: Success, 120ms
# [INFO] Running HTTP probe for google.com
# [INFO] HTTP probe completed: Success, 200ms
```

### Step 4: View Results

```bash
# Check probe runs in database
docker compose exec postgres psql -U observability -d observability_dns -c \
  "SELECT domain_id, check_type, success, total_ms, completed_at FROM probe_runs ORDER BY completed_at DESC LIMIT 10;"

# View incidents
docker compose exec postgres psql -U observability -d observability_dns -c \
  "SELECT * FROM incidents WHERE status = 'OPEN';"
```

### Step 5: Access UI Dashboard

Open http://localhost:3000 in your browser to see:
- Domain list with health status
- Probe run history
- Incident timeline
- Charts showing latency trends

## Testing Different Scenarios

### Test 1: Healthy Domain Monitoring

**Domain**: `google.com`
**Expected**: All checks pass, low latency, no incidents

```bash
# Add domain
curl -X POST http://localhost:5000/api/domains \
  -d '{"name": "google.com", "intervalMinutes": 1, "enabled": true}'

# Wait 1-2 minutes, then check results
curl http://localhost:5000/api/domains/{domain-id}/probe-runs
```

### Test 2: Expired Certificate Detection

**Domain**: `expired.badssl.com`
**Expected**: TLS check fails, incident created, alert sent

```bash
# Add domain
curl -X POST http://localhost:5000/api/domains \
  -d '{"name": "expired.badssl.com", "intervalMinutes": 5, "enabled": true}'

# Check for incidents after probe runs
curl http://localhost:5000/api/incidents?domainId={domain-id}
```

### Test 3: DNS Failure Detection

**Domain**: `nonexistent-domain-12345.com`
**Expected**: DNS check fails with NXDOMAIN, incident created

```bash
# Add domain
curl -X POST http://localhost:5000/api/domains \
  -d '{"name": "nonexistent-domain-12345.com", "intervalMinutes": 5, "enabled": true}'

# Check probe runs for DNS failures
curl http://localhost:5000/api/domains/{domain-id}/probe-runs?checkType=DNS
```

### Test 4: HTTP Error Detection

**Domain**: `httpstat.us/500` (returns 500 error)
**Expected**: HTTP check fails, incident created

```bash
# Note: You may need to modify HTTP probe to handle status codes
curl -X POST http://localhost:5000/api/domains \
  -d '{"name": "httpstat.us", "intervalMinutes": 5, "enabled": true}'
```

### Test 5: Latency Spike Detection

**Domain**: `httpstat.us/200?sleep=5000` (5 second delay)
**Expected**: High latency detected, alert if threshold exceeded

## Testing Alert Rules

### Create Alert Rule

```bash
# Create alert rule: "3 fails in last 5 runs"
curl -X POST http://localhost:5000/api/domains/{domain-id}/alert-rules \
  -H "Content-Type: application/json" \
  -d '{
    "checkType": "HTTP",
    "triggerCondition": "3 fails in 5 runs",
    "enabled": true
  }'
```

### Test Alert Triggering

1. Add a domain that will fail (e.g., `expired.badssl.com`)
2. Wait for 3+ probe failures
3. Check `notifications` table for pending alerts
4. Verify Slack/Email alerts are sent

```bash
# Check notifications
docker compose exec postgres psql -U observability -d observability_dns -c \
  "SELECT * FROM notifications WHERE status = 'PENDING' ORDER BY created_at DESC;"
```

## Monitoring Your Own Infrastructure

### Option 1: Monitor Your Deployed API

If you deploy this observability system to Render/Azure/AWS:

1. Get your public URL: `https://your-api.onrender.com`
2. Add it as a monitored domain
3. Monitor its own health!

```bash
curl -X POST http://localhost:5000/api/domains \
  -d '{"name": "your-api.onrender.com", "intervalMinutes": 5, "enabled": true}'
```

### Option 2: Create Test Endpoints

Create simple endpoints to test:

```bash
# On your server, create a test endpoint
# Example: https://your-domain.com/health
# Returns: {"status": "healthy", "timestamp": "..."}

# Monitor it
curl -X POST http://localhost:5000/api/domains \
  -d '{"name": "your-domain.com", "intervalMinutes": 1, "enabled": true}'
```

### Option 3: Monitor Local Services (Advanced)

If you want to monitor services on your local network:

1. Use your public IP or domain
2. Ensure ports are accessible
3. Add to monitoring

## Validation Checklist

After implementation, verify:

- [ ] Can add domains via API
- [ ] Worker runs probes on schedule
- [ ] DNS checks resolve correctly
- [ ] TLS checks validate certificates
- [ ] HTTP checks return status codes
- [ ] Probe results stored in database
- [ ] Incidents created on failures
- [ ] Alerts sent via Slack/Email
- [ ] UI displays domain health
- [ ] Charts show latency trends
- [ ] Incident timeline works

## Troubleshooting

### Probes Not Running

```bash
# Check worker logs
docker compose logs worker

# Verify domains are enabled
docker compose exec postgres psql -U observability -d observability_dns -c \
  "SELECT name, enabled, interval_minutes FROM domains;"

# Check Quartz scheduler
docker compose exec worker dotnet --version  # Verify .NET is working
```

### No Results in Database

```bash
# Check if probe_runs table has data
docker compose exec postgres psql -U observability -d observability_dns -c \
  "SELECT COUNT(*) FROM probe_runs;"

# Check worker is writing to database
docker compose logs worker | grep -i "probe\|error\|exception"
```

### Alerts Not Sending

```bash
# Check notifications table
docker compose exec postgres psql -U observability -d observability_dns -c \
  "SELECT * FROM notifications WHERE status = 'PENDING';"

# Check notification attempts
docker compose exec postgres psql -U observability -d observability_dns -c \
  "SELECT * FROM notification_attempts ORDER BY attempted_at DESC LIMIT 10;"

# Verify Slack webhook URL is set
docker compose exec api env | grep SLACK
```

## Best Practices for Testing

1. **Start Small**: Monitor 2-3 domains first
2. **Use Short Intervals**: Use 1-minute intervals for faster testing
3. **Mix Healthy and Unhealthy**: Test both success and failure scenarios
4. **Monitor Logs**: Watch worker logs during testing
5. **Check Database**: Verify data is being written correctly
6. **Test Alerts**: Intentionally break a domain to test alerting
7. **Use BadSSL**: Leverage badssl.com for TLS testing
8. **Monitor Your Own**: Add your own endpoints for real-world testing

## Example Test Script

```bash
#!/bin/bash
# test-domains.sh - Add test domains for monitoring

API_URL="http://localhost:5000/api/domains"

# Add reliable domains
curl -X POST $API_URL -H "Content-Type: application/json" \
  -d '{"name": "google.com", "intervalMinutes": 5, "enabled": true}'

curl -X POST $API_URL -H "Content-Type: application/json" \
  -d '{"name": "github.com", "intervalMinutes": 5, "enabled": true}'

# Add TLS test domains
curl -X POST $API_URL -H "Content-Type: application/json" \
  -d '{"name": "expired.badssl.com", "intervalMinutes": 5, "enabled": true}'

curl -X POST $API_URL -H "Content-Type: application/json" \
  -d '{"name": "sha256.badssl.com", "intervalMinutes": 5, "enabled": true}'

echo "Test domains added. Check worker logs: docker compose logs -f worker"
```

Save as `test-domains.sh`, make executable (`chmod +x test-domains.sh`), and run after API is implemented.
