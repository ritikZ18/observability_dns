# API Reference

## Base URL

- **Local**: `http://localhost:5000`
- **Production**: `https://your-api.onrender.com`

## Health Endpoints

### GET /healthz

Liveness probe - indicates service is running.

**Response**: `200 OK`
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### GET /readyz

Readiness probe - indicates service can handle requests (database accessible).

**Response**: `200 OK`
```json
{
  "status": "Ready",
  "checks": {
    "database": "Healthy"
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Domain Management

### GET /api/domains

Get all domains.

**Response**: `200 OK`
```json
[
  {
    "id": "uuid",
    "name": "example.com",
    "enabled": true,
    "intervalMinutes": 5,
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

### GET /api/domains/{id}

Get domain by ID.

**Response**: `200 OK`
```json
{
  "id": "uuid",
  "name": "example.com",
  "enabled": true,
  "intervalMinutes": 5,
  "checks": [
    {
      "id": "uuid",
      "checkType": "DNS",
      "enabled": true
    }
  ],
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### POST /api/domains

Create a new domain.

**Request Body**:
```json
{
  "name": "example.com",
  "intervalMinutes": 5,
  "enabled": true
}
```

**Response**: `201 Created`
```json
{
  "id": "uuid",
  "name": "example.com",
  "enabled": true,
  "intervalMinutes": 5,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### PUT /api/domains/{id}

Update domain.

**Request Body**:
```json
{
  "enabled": false,
  "intervalMinutes": 15
}
```

**Response**: `200 OK`

### DELETE /api/domains/{id}

Delete domain.

**Response**: `204 No Content`

## Probe Runs

### GET /api/domains/{id}/probe-runs

Get probe runs for a domain.

**Query Parameters**:
- `checkType` (optional): Filter by check type (DNS, TLS, HTTP)
- `limit` (optional): Number of results (default: 50)
- `offset` (optional): Pagination offset

**Response**: `200 OK`
```json
[
  {
    "id": "uuid",
    "checkType": "HTTP",
    "success": true,
    "totalMs": 150,
    "ttfbMs": 100,
    "statusCode": 200,
    "completedAt": "2024-01-01T00:00:00Z"
  }
]
```

## Incidents

### GET /api/incidents

Get all incidents.

**Query Parameters**:
- `status` (optional): Filter by status (OPEN, RESOLVED, ACKNOWLEDGED)
- `domainId` (optional): Filter by domain

**Response**: `200 OK`
```json
[
  {
    "id": "uuid",
    "domainId": "uuid",
    "domainName": "example.com",
    "checkType": "TLS",
    "severity": "HIGH",
    "status": "OPEN",
    "reason": "Certificate expires in 7 days",
    "startedAt": "2024-01-01T00:00:00Z"
  }
]
```

### GET /api/incidents/{id}

Get incident by ID.

**Response**: `200 OK`

### PUT /api/incidents/{id}/resolve

Resolve an incident.

**Response**: `200 OK`

## Alert Rules

### GET /api/domains/{id}/alert-rules

Get alert rules for a domain.

**Response**: `200 OK`

### POST /api/domains/{id}/alert-rules

Create alert rule.

**Request Body**:
```json
{
  "checkType": "HTTP",
  "triggerCondition": "3 fails in 5 runs",
  "enabled": true
}
```

**Response**: `201 Created`

## Error Responses

### 400 Bad Request
```json
{
  "error": "Validation failed",
  "details": ["Field 'name' is required"]
}
```

### 404 Not Found
```json
{
  "error": "Domain not found"
}
```

### 500 Internal Server Error
```json
{
  "error": "Internal server error",
  "requestId": "uuid"
}
```

## Authentication

Currently, the API is unauthenticated. Future versions will support:
- JWT tokens
- API keys
- OAuth2

## Rate Limiting

Not implemented in MVP. Future versions will include rate limiting.
