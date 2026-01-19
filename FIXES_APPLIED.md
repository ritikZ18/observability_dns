# Fixes Applied to Resolve 404 and Worker Issues

## Issues Found and Fixed

### 1. API Route Fix (404 Error)
**Problem**: The UI was calling `/api/probe-runs/domains/{id}` but ASP.NET Core controller route wasn't matching.

**Fix**: Changed the route attribute in `ProbeRunsController.cs` from:
```csharp
[Route("api/[controller]")]
```
to:
```csharp
[Route("api/probe-runs")]
```

This ensures the route explicitly matches what the UI is calling.

### 2. CheckType Enum Mismatch
**Problem**: The API was returning enum values as strings ("DNS", "TLS", "HTTP") due to `JsonStringEnumConverter()`, but the TypeScript enum was defined as numeric (0, 1, 2).

**Fix**: Updated `ui/src/types/index.ts` to use string enum values:
```typescript
export enum CheckType {
  DNS = 'DNS',
  TLS = 'TLS',
  HTTP = 'HTTP'
}
```

This matches what the API sends.

### 3. Worker Status
**Status**: Worker is running correctly and scheduling probe jobs. Verified:
- Worker starts successfully
- Quartz scheduler initializes
- Probe jobs are scheduled for all enabled domains
- Probe runs are being created in the database

## Verification

After these fixes:
1. ✅ API endpoint `/api/probe-runs/domains/{domainId}` returns data
2. ✅ Worker is running and creating probe runs
3. ✅ 9 probe runs exist in database (3 domains × 3 check types)
4. ✅ UI enum types match API response format

## Testing

To verify everything works:
1. Check worker logs: `docker compose logs worker`
2. Check API: `curl http://localhost:5000/api/probe-runs/domains/{domain-id}`
3. Check database: `docker compose exec postgres psql -U observability -d observability_dns -c "SELECT * FROM probe_runs LIMIT 5;"`

## Next Steps

The UI should now:
- Successfully fetch probe runs from the API
- Display status for DNS, TLS, and HTTP checks
- Show "Last Check" timestamps
- Display proper status indicators
