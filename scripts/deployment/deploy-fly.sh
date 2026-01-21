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
fly postgres create --name observability-dns-db --region ams --vm-size shared-cpu-1x --volume-size 1 2>/dev/null || echo "Database already exists"

echo ""
echo "⏳ Waiting for database to be ready (30 seconds)..."
sleep 30

echo ""
echo "🔧 Step 2: Creating and deploying API app..."
fly apps create observability-dns-api 2>/dev/null || echo "API app already exists"

echo "🔗 Attaching database to API..."
fly postgres attach --app observability-dns-api observability-dns-db 2>/dev/null || echo "Database already attached"

echo "🚀 Deploying API..."
fly deploy --config fly.toml --app observability-dns-api

echo ""
echo "🔧 Step 3: Creating and deploying Worker app..."
fly apps create observability-dns-worker 2>/dev/null || echo "Worker app already exists"

echo "🔗 Attaching database to Worker..."
fly postgres attach --app observability-dns-worker observability-dns-db 2>/dev/null || echo "Database already attached"

echo "🚀 Deploying Worker..."
fly deploy --config fly.worker.toml --app observability-dns-worker

echo ""
echo "🔧 Step 4: Creating and deploying UI app..."
fly apps create observability-dns-ui 2>/dev/null || echo "UI app already exists"

echo "🔧 Setting API URL..."
fly secrets set VITE_API_URL=https://observability-dns-api.fly.dev --app observability-dns-ui

echo "🚀 Deploying UI..."
fly deploy --config fly.ui.toml --app observability-dns-ui

echo ""
echo "📦 Step 5: Running database migrations..."
echo "   (Running from local machine since production container doesn't have .NET SDK)"

# Get DATABASE_URL from Fly.io secrets
# Try multiple methods to get the connection string
DB_URL=$(fly secrets list --app observability-dns-api 2>/dev/null | grep "DATABASE_URL" | awk '{print $2}' | head -1)

# If empty, try getting from postgres connection info
if [ -z "$DB_URL" ] || [ "$DB_URL" = "DATABASE_URL" ]; then
    echo "⚠️  Could not get DATABASE_URL from secrets"
    echo "   Trying alternative method..."
    # Get connection string from postgres attachment
    DB_URL=$(fly postgres connect --app observability-dns-db 2>&1 | grep -oP 'postgres://[^\s]+' | head -1 || echo "")
fi

if [ -z "$DB_URL" ] || [ "$DB_URL" = "DATABASE_URL" ]; then
    echo "⚠️  Could not get DATABASE_URL automatically"
    echo ""
    echo "   Please run migrations manually:"
    echo "   1. Get connection string:"
    echo "      fly secrets list --app observability-dns-api"
    echo "   2. Run migrations:"
    echo "      ./run-fly-migrations.sh"
    echo ""
    echo "   Or set it manually:"
    echo "      export ConnectionStrings__DefaultConnection=\"postgres://user:pass@host:port/db\""
    echo "      dotnet ef database update --project src/domain --startup-project src/api"
else
    echo "✅ Found database URL"
    
    # Check if .NET SDK is available locally
    if command -v dotnet &> /dev/null; then
        # Check if EF Core tools are installed
        if ! dotnet ef --version &> /dev/null; then
            echo "📦 Installing EF Core tools..."
            dotnet tool install --global dotnet-ef
            export PATH="$PATH:$HOME/.dotnet/tools"
        fi
        
        echo "🚀 Running migrations..."
        cd /home/swamizero/observability_dns
        
        # Try with --connection flag first
        echo "   Attempting migration with connection string..."
        if dotnet ef database update --project src/domain --startup-project src/api --connection "$DB_URL" 2>&1; then
            echo "✅ Migrations completed successfully!"
        else
            echo "⚠️  Migration with --connection flag failed"
            echo "   Trying with environment variable..."
            # Try setting as environment variable instead
            export ConnectionStrings__DefaultConnection="$DB_URL"
            if dotnet ef database update --project src/domain --startup-project src/api 2>&1; then
                echo "✅ Migrations completed successfully!"
            else
                echo "⚠️  Migration failed with both methods"
                echo "   Please run manually: ./run-fly-migrations.sh"
                echo "   Or check connection string format"
            fi
        fi
    else
        echo "⚠️  .NET SDK not found locally"
        echo "   Please install .NET 8 SDK and run: ./run-fly-migrations.sh"
    fi
fi

echo ""
echo "🔧 Step 6: Ensuring UI is running (fixing suspended machines)..."
fly scale count 1 --app observability-dns-ui
fly apps restart observability-dns-ui

echo ""
echo "⏳ Waiting 15 seconds for services to be ready..."
sleep 15

echo ""
echo "✅ Deployment complete!"
echo ""
echo "📋 Your services:"
echo "  API: https://observability-dns-api.fly.dev"
echo "  UI: https://observability-dns-ui.fly.dev"
echo ""
echo "🔍 Verifying deployment..."

# Check API health
echo ""
echo "Checking API health..."
API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" https://observability-dns-api.fly.dev/healthz || echo "000")
if [ "$API_HEALTH" = "200" ]; then
    echo "  ✅ API is healthy (HTTP $API_HEALTH)"
else
    echo "  ⚠️  API returned HTTP $API_HEALTH"
fi

# Check UI
echo ""
echo "Checking UI..."
UI_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" https://observability-dns-ui.fly.dev || echo "000")
if [ "$UI_HEALTH" = "200" ]; then
    echo "  ✅ UI is responding (HTTP $UI_HEALTH)"
else
    echo "  ⚠️  UI returned HTTP $UI_HEALTH"
    echo "     It may take a minute to wake up. Try: fly apps restart observability-dns-ui"
fi

echo ""
echo "📝 Next steps:"
echo "  1. Open UI: https://observability-dns-ui.fly.dev"
echo "  2. Check logs if issues: fly logs --app observability-dns-api"
echo "  3. View status: fly status --app observability-dns-api"
echo ""
echo "🎉 Done!"
