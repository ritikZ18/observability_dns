# Testing Guide

Comprehensive guide for testing the DNS & TLS Observatory project.

## Table of Contents

1. [Test Types](#test-types)
2. [Running Tests](#running-tests)
3. [Writing Tests](#writing-tests)
4. [Test Coverage](#test-coverage)
5. [CI/CD Integration](#cicd-integration)

---

## Test Types

### Unit Tests

**Location**: `tests/unit/`  
**Framework**: xUnit, Moq, FluentAssertions  
**Purpose**: Test individual components in isolation

**Example**:
```csharp
[Fact]
public void DomainService_GetAllDomains_ReturnsList()
{
    // Arrange
    var mockDb = new Mock<ObservabilityDnsDbContext>();
    var service = new DomainService(mockDb.Object);
    
    // Act
    var result = service.GetAllDomainsAsync();
    
    // Assert
    result.Should().NotBeNull();
}
```

### Integration Tests

**Location**: `tests/integration/`  
**Framework**: xUnit, TestContainers  
**Purpose**: Test API endpoints with real database

**Example**:
```csharp
[Fact]
public async Task GetDomains_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/domains");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### E2E Tests

**Location**: `tests/e2e/`  
**Framework**: Playwright  
**Purpose**: Test full user workflows

**Example**:
```typescript
test('should add domain', async ({ page }) => {
  await page.goto('/');
  await page.fill('input[placeholder*="Domain"]', 'example.com');
  await page.click('button:has-text("ADD DOMAIN")');
  await expect(page.locator('text=example.com')).toBeVisible();
});
```

### Smoke Tests

**Location**: `tests/smoke/`  
**Framework**: Shell scripts  
**Purpose**: Quick health checks after deployment

**Run**:
```bash
./tests/smoke/run-smoke-tests.sh production
```

### UAT Tests

**Location**: `tests/uat/`  
**Format**: Manual test cases  
**Purpose**: User acceptance validation

See [UAT_TEST_CASES.md](../tests/uat/UAT_TEST_CASES.md)

---

## Running Tests

### Backend Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/unit/

# Integration tests only
dotnet test tests/integration/

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Frontend E2E Tests

```bash
cd tests/e2e
npm install
npm test

# Interactive mode
npm run test:ui

# Debug mode
npm run test:debug
```

### Smoke Tests

```bash
# Local environment
./tests/smoke/run-smoke-tests.sh local

# Fly.io deployment
./tests/smoke/run-smoke-tests.sh fly

# Production
./tests/smoke/run-smoke-tests.sh production
```

---

## Writing Tests

### Unit Test Structure

```csharp
public class DomainServiceTests
{
    private readonly Mock<ObservabilityDnsDbContext> _mockDb;
    private readonly DomainService _service;
    
    public DomainServiceTests()
    {
        _mockDb = new Mock<ObservabilityDnsDbContext>();
        _service = new DomainService(_mockDb.Object);
    }
    
    [Fact]
    public void TestName_Scenario_ExpectedResult()
    {
        // Arrange
        
        // Act
        
        // Assert
    }
}
```

### Integration Test Structure

```csharp
public class DomainsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public DomainsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task GetDomains_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/domains");
        response.EnsureSuccessStatusCode();
    }
}
```

### E2E Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test('test name', async ({ page }) => {
    await page.goto('/');
    // Test steps
    await expect(page.locator('selector')).toBeVisible();
  });
});
```

---

## Test Coverage

### Coverage Goals

- **Unit Tests**: >80% code coverage
- **Integration Tests**: >70% API endpoint coverage
- **E2E Tests**: Critical user paths only

### Generating Coverage Reports

```bash
# Backend coverage
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage

# Frontend coverage (if using Jest)
npm test -- --coverage
```

---

## CI/CD Integration

### GitHub Actions

Tests run automatically:
- **Unit tests**: On every push
- **Integration tests**: On pull requests
- **E2E tests**: On merge to main
- **Smoke tests**: After deployment

### Local Pre-commit

```bash
# Run tests before committing
./scripts/pre-commit.sh
```

---

## Best Practices

1. **Arrange-Act-Assert**: Follow AAA pattern
2. **Test Isolation**: Each test should be independent
3. **Descriptive Names**: Test names should describe what they test
4. **One Assertion**: One concept per test
5. **Mock External Dependencies**: Don't hit real APIs/databases in unit tests
6. **Fast Tests**: Unit tests should run quickly
7. **Maintainable**: Tests should be easy to understand and modify

---

## Troubleshooting

### Tests Failing Locally

1. Check database connection
2. Verify environment variables
3. Check test data setup
4. Review test logs

### E2E Tests Failing

1. Check if UI is running
2. Verify API is accessible
3. Check browser compatibility
4. Review screenshots/videos

### Smoke Tests Failing

1. Verify deployment is complete
2. Check service URLs
3. Verify network connectivity
4. Review service logs

---

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Playwright Documentation](https://playwright.dev/)
- [TestContainers Documentation](https://testcontainers.com/)
- [FluentAssertions Documentation](https://fluentassertions.com/)

---

## See Also

- [UAT Guide](./UAT.md) - User Acceptance Testing
- [Smoke Tests Guide](./SMOKE_TESTS.md) - Smoke testing documentation
- [Test Cases](../tests/uat/UAT_TEST_CASES.md) - Manual test cases
