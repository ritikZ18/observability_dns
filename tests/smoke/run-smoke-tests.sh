#!/bin/bash

# Smoke Tests for DNS & TLS Observatory
# Quick health checks after deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default environment
ENV="${1:-local}"
API_URL=""
UI_URL=""

# Set URLs based on environment
case "$ENV" in
  local)
    API_URL="http://localhost:5000"
    UI_URL="http://localhost:3002"
    ;;
  fly)
    API_URL="https://observability-dns-api.fly.dev"
    UI_URL="https://observability-dns-ui.fly.dev"
    ;;
  production|prod)
    API_URL="${API_URL:-https://observability-dns-api.fly.dev}"
    UI_URL="${UI_URL:-https://observability-dns-ui.fly.dev}"
    ;;
  *)
    echo "Usage: $0 [local|fly|production]"
    exit 1
    ;;
esac

echo "🧪 Running Smoke Tests"
echo "Environment: $ENV"
echo "API URL: $API_URL"
echo "UI URL: $UI_URL"
echo ""

PASSED=0
FAILED=0

# Test function
test_endpoint() {
    local name=$1
    local url=$2
    local expected_status=${3:-200}
    
    echo -n "Testing $name... "
    
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$url" || echo "000")
    
    if [ "$HTTP_CODE" = "$expected_status" ]; then
        echo -e "${GREEN}✓ PASS${NC} (HTTP $HTTP_CODE)"
        ((++PASSED))
        return 0
    else
        echo -e "${RED}✗ FAIL${NC} (Expected $expected_status, got $HTTP_CODE)"
        ((++FAILED))
        return 1
    fi
}

# API Health Checks
echo "📡 API Tests"
echo "------------"

test_endpoint "API Health Check" "$API_URL/healthz" 200
test_endpoint "API Readiness" "$API_URL/readyz" 200
test_endpoint "API Swagger" "$API_URL/swagger/index.html" 200

# API Endpoints
echo ""
echo "📋 API Endpoints"
echo "----------------"

test_endpoint "Get Domains" "$API_URL/api/domains" 200
test_endpoint "Get Groups" "$API_URL/api/groups" 200
test_endpoint "Speed Test Ping" "$API_URL/api/speed-test/ping" 204
test_endpoint "Speed Test Download" "$API_URL/api/speed-test/download?bytes=1024" 200

echo -n "Testing Speed Test Upload... "
UPLOAD_RESPONSE=$(printf 'local-speed-test' | curl -sS -X POST \
  -H "Content-Type: application/octet-stream" \
  --data-binary @- "$API_URL/api/speed-test/upload" || true)
if [[ "$UPLOAD_RESPONSE" == *'"bytesReceived":16'* ]]; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((++PASSED))
else
    echo -e "${RED}✗ FAIL${NC} (Unexpected response: $UPLOAD_RESPONSE)"
    ((++FAILED))
fi

# UI Tests
echo ""
echo "🌐 UI Tests"
echo "-----------"

test_endpoint "UI Homepage" "$UI_URL" 200
test_endpoint "UI Speed Test Route" "$UI_URL/speed-test" 200

# Summary
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo -e "${GREEN}Passed: $PASSED${NC}"
if [ $FAILED -gt 0 ]; then
    echo -e "${RED}Failed: $FAILED${NC}"
    exit 1
else
    echo -e "${GREEN}Failed: $FAILED${NC}"
    echo ""
    echo "✅ All smoke tests passed!"
    exit 0
fi
