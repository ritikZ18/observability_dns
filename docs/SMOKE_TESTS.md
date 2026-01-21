# Smoke Tests Documentation

## Overview

Smoke tests are quick, automated tests that verify basic functionality after deployment. They ensure the system is up and running correctly.

## Purpose

- Verify deployment was successful
- Check critical endpoints are accessible
- Ensure basic functionality works
- Quick health check before full testing

## Running Smoke Tests

### Local Environment

```bash
./tests/smoke/run-smoke-tests.sh local
```

### Fly.io Deployment

```bash
./tests/smoke/run-smoke-tests.sh fly
```

### Production

```bash
./tests/smoke/run-smoke-tests.sh production
```

### Custom Environment

```bash
API_URL=https://api.example.com UI_URL=https://ui.example.com ./tests/smoke/run-smoke-tests.sh
```

## Test Coverage

### API Tests

- ✅ Health check endpoint (`/healthz`)
- ✅ Readiness endpoint (`/readyz`)
- ✅ Swagger documentation (`/swagger`)
- ✅ Get domains (`/api/domains`)
- ✅ Get groups (`/api/groups`)

### UI Tests

- ✅ Homepage loads
- ✅ UI is accessible

## Test Scripts

### Main Script

`tests/smoke/run-smoke-tests.sh` - Runs all smoke tests

### API-Specific Script

`tests/smoke/test-api.sh` - Tests API endpoints only

## CI/CD Integration

Smoke tests run automatically after deployment:

```yaml
# GitHub Actions example
- name: Run Smoke Tests
  run: ./tests/smoke/run-smoke-tests.sh production
```

## Expected Results

### Success

```
🧪 Running Smoke Tests
Environment: production
API URL: https://observability-dns-api.fly.dev
UI URL: https://observability-dns-ui.fly.dev

📡 API Tests
------------
Testing API Health Check... ✓ PASS (HTTP 200)
Testing API Readiness... ✓ PASS (HTTP 200)
Testing API Swagger... ✓ PASS (HTTP 200)

📋 API Endpoints
----------------
Testing Get Domains... ✓ PASS (HTTP 200)
Testing Get Groups... ✓ PASS (HTTP 200)

🌐 UI Tests
-----------
Testing UI Homepage... ✓ PASS (HTTP 200)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Summary
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Passed: 6
Failed: 0

✅ All smoke tests passed!
```

### Failure

If tests fail, check:
1. Services are running
2. URLs are correct
3. Network connectivity
4. Service logs

## Troubleshooting

### API Not Responding

```bash
# Check API status
curl https://observability-dns-api.fly.dev/healthz

# Check logs
fly logs --app observability-dns-api
```

### UI Not Loading

```bash
# Check UI status
curl -I https://observability-dns-ui.fly.dev

# Check logs
fly logs --app observability-dns-ui
```

## Adding New Tests

To add new smoke tests, edit `tests/smoke/run-smoke-tests.sh`:

```bash
# Add new test
test_endpoint "New Feature" "$API_URL/api/new-endpoint" 200
```

## Best Practices

1. **Keep Tests Fast**: Smoke tests should complete in < 30 seconds
2. **Test Critical Paths Only**: Don't test everything
3. **Make Tests Reliable**: Avoid flaky tests
4. **Clear Output**: Make results easy to understand
5. **Fail Fast**: Stop on first failure if needed

## See Also

- [Testing Guide](./TESTING.md)
- [UAT Guide](./UAT.md)
