# Test Suite

This directory contains all tests for the DNS & TLS Observatory project.

## Structure

```
tests/
├── unit/              # Unit tests (backend)
├── integration/       # Integration tests (API + Database)
├── e2e/              # End-to-end tests (UI + API)
├── smoke/             # Smoke tests (quick health checks)
├── uat/               # User Acceptance Tests (manual test cases)
└── performance/       # Performance/load tests
```

## Test Types

### Unit Tests (`tests/unit/`)
- **Purpose**: Test individual components in isolation
- **Framework**: xUnit, Moq
- **Coverage**: Services, Domain logic, Utilities
- **Run**: `dotnet test tests/unit/`

### Integration Tests (`tests/integration/`)
- **Purpose**: Test API endpoints with real database
- **Framework**: xUnit, TestContainers (PostgreSQL)
- **Coverage**: API controllers, Database operations
- **Run**: `dotnet test tests/integration/`

### E2E Tests (`tests/e2e/`)
- **Purpose**: Test full user workflows
- **Framework**: Playwright (TypeScript)
- **Coverage**: UI flows, API integration
- **Run**: `npm test` (in tests/e2e)

### Smoke Tests (`tests/smoke/`)
- **Purpose**: Quick health checks after deployment
- **Framework**: Shell scripts, curl
- **Coverage**: Basic API endpoints, UI loading
- **Run**: `./tests/smoke/run-smoke-tests.sh`

### UAT (`tests/uat/`)
- **Purpose**: Manual test cases for acceptance
- **Format**: Markdown test cases
- **Coverage**: User stories, feature validation
- **Run**: Manual execution

### Performance Tests (`tests/performance/`)
- **Purpose**: Load testing, performance benchmarks
- **Framework**: k6, Artillery
- **Coverage**: API load, concurrent requests
- **Run**: `k6 run tests/performance/load-test.js`

## Running Tests

### All Tests
```bash
# Backend tests
dotnet test

# Frontend E2E tests
cd tests/e2e && npm test

# Smoke tests
./tests/smoke/run-smoke-tests.sh
```

### Specific Test Suites
```bash
# Unit tests only
dotnet test tests/unit/

# Integration tests only
dotnet test tests/integration/

# E2E tests only
cd tests/e2e && npm test

# Smoke tests
./tests/smoke/run-smoke-tests.sh --env production
```

## CI/CD Integration

Tests run automatically in GitHub Actions:
- Unit tests: On every push
- Integration tests: On PR
- E2E tests: On merge to main
- Smoke tests: After deployment

## Test Coverage

Target coverage:
- Unit tests: >80%
- Integration tests: >70%
- E2E tests: Critical paths only

## See Also

- [Testing Guide](../docs/TESTING.md) - Comprehensive testing documentation
- [UAT Guide](../docs/UAT.md) - User Acceptance Testing guide
- [Smoke Tests](../docs/SMOKE_TESTS.md) - Smoke testing documentation
