#!/bin/bash

# API-specific smoke tests

set -e

API_URL="${API_URL:-http://localhost:5000}"

echo "🧪 API Smoke Tests"
echo "API URL: $API_URL"
echo ""

# Test health endpoints
echo "Testing health endpoints..."
curl -f "$API_URL/healthz" > /dev/null && echo "✓ Health check passed"
curl -f "$API_URL/readyz" > /dev/null && echo "✓ Readiness check passed"

# Test API endpoints
echo ""
echo "Testing API endpoints..."
curl -f "$API_URL/api/domains" > /dev/null && echo "✓ GET /api/domains passed"
curl -f "$API_URL/api/groups" > /dev/null && echo "✓ GET /api/groups passed"

# Test creating a domain
echo ""
echo "Testing domain creation..."
RESPONSE=$(curl -s -X POST "$API_URL/api/domains" \
  -H "Content-Type: application/json" \
  -d '{"name":"test.example.com","intervalMinutes":5,"enabled":true}' || echo "")

if [ -n "$RESPONSE" ]; then
    echo "✓ POST /api/domains passed"
else
    echo "✗ POST /api/domains failed"
    exit 1
fi

echo ""
echo "✅ All API smoke tests passed!"
