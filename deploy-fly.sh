#!/bin/bash

set -e

echo "🚀 Deploying DNS & TLS Observatory to Fly.io"
echo ""

# Check if fly CLI is installed
if ! command -v fly &> /dev/null; then
    echo "❌ Fly CLI not found. Install it first:"
    echo "   curl -L https://fly.io/install.sh | sh"
    exit 1
fi

# Check if logged in
if ! fly auth whoami &> /dev/null; then
    echo "🔐 Please login to Fly.io:"
    fly auth login
fi

echo "📦 Step 1: Creating PostgreSQL database..."
fly postgres create --name observability-dns-db --region ams --vm-size shared-cpu-1x --volume-size 1 || echo "Database already exists"

echo ""
echo "⏳ Waiting for database to be ready (30 seconds)..."
sleep 30

echo ""
echo "🔧 Step 2: Creating API app..."
fly apps create observability-dns-api 2>/dev/null || echo "API app already exists"

echo "🔗 Attaching database to API..."
fly postgres attach --app observability-dns-api observability-dns-db 2>/dev/null || echo "Database already attached"

echo "🚀 Deploying API..."
fly deploy --config fly.toml --app observability-dns-api

echo ""
echo "🔧 Step 3: Creating Worker app..."
fly apps create observability-dns-worker 2>/dev/null || echo "Worker app already exists"

echo "🔗 Attaching database to Worker..."
fly postgres attach --app observability-dns-worker observability-dns-db 2>/dev/null || echo "Database already attached"

echo "🚀 Deploying Worker..."
fly deploy --config fly.worker.toml --app observability-dns-worker

echo ""
echo "🔧 Step 4: Creating UI app..."
fly apps create observability-dns-ui 2>/dev/null || echo "UI app already exists"

# Get API URL
API_URL=$(fly status --app observability-dns-api 2>/dev/null | grep "Hostname" | awk '{print $2}' || echo "observability-dns-api.fly.dev")
API_URL="https://${API_URL}"

echo "🔧 Setting API URL: $API_URL"
fly secrets set VITE_API_URL="$API_URL" --app observability-dns-ui

echo "🚀 Deploying UI..."
fly deploy --config fly.ui.toml --app observability-dns-ui

echo ""
echo "✅ Deployment complete!"
echo ""
echo "📋 Your services:"
echo "  API: https://observability-dns-api.fly.dev"
echo "  UI: https://observability-dns-ui.fly.dev"
echo ""
echo "📝 Next steps:"
echo "  1. Run migrations:"
echo "     fly ssh console --app observability-dns-api"
echo "     cd /app && dotnet ef database update --project ../domain --startup-project ."
echo ""
echo "  2. Check health:"
echo "     curl https://observability-dns-api.fly.dev/healthz"
echo ""
echo "  3. View logs:"
echo "     fly logs --app observability-dns-api"
