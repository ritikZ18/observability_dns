#!/bin/bash

set -e

echo "🔗 Getting database connection string from Fly.io..."

# Get DATABASE_URL from Fly.io secrets
DB_URL=$(fly secrets list --app observability-dns-api 2>/dev/null | grep "DATABASE_URL" | awk '{print $2}')

if [ -z "$DB_URL" ]; then
    echo "❌ Could not find DATABASE_URL in secrets"
    echo ""
    echo "Trying alternative method..."
    # Try to get from postgres attachment
    DB_URL=$(fly postgres connect --app observability-dns-db 2>&1 | grep "postgres://" | head -1 || echo "")
    
    if [ -z "$DB_URL" ]; then
        echo "❌ Could not get database URL"
        echo ""
        echo "Manual steps:"
        echo "1. Get DATABASE_URL: fly secrets list --app observability-dns-api"
        echo "2. Export it: export ConnectionStrings__DefaultConnection=\"<DATABASE_URL>\""
        echo "3. Run: dotnet ef database update --project src/domain --startup-project src/api"
        exit 1
    fi
fi

echo "✅ Found database URL"
echo "📦 Running migrations..."

# Set connection string
export ConnectionStrings__DefaultConnection="$DB_URL"

# Navigate to project root
cd /home/swamizero/observability_dns

# Check if .NET SDK is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8 SDK"
    exit 1
fi

# Check if EF Core tools are installed
if ! dotnet ef --version &> /dev/null; then
    echo "📦 Installing EF Core tools..."
    dotnet tool install --global dotnet-ef
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

# Run migrations
echo "🚀 Running database migrations..."
echo "   Connection string format: postgres:// (Fly.io standard)"

# Try with --connection flag first (recommended for postgres:// format)
if dotnet ef database update --project src/domain --startup-project src/api --connection "$DB_URL" 2>&1; then
    echo ""
    echo "✅ Migrations completed successfully!"
else
    echo ""
    echo "⚠️  Migration with --connection flag failed, trying environment variable..."
    # Fallback: set as environment variable
    export ConnectionStrings__DefaultConnection="$DB_URL"
    if dotnet ef database update --project src/domain --startup-project src/api 2>&1; then
        echo ""
        echo "✅ Migrations completed successfully!"
    else
        echo ""
        echo "❌ Migration failed with both methods"
        echo ""
        echo "Troubleshooting:"
        echo "1. Check connection string format:"
        echo "   echo \"\$DB_URL\""
        echo ""
        echo "2. Test connection manually:"
        echo "   psql \"\$DB_URL\""
        echo ""
        echo "3. If connection string has special characters, try:"
        echo "   export ConnectionStrings__DefaultConnection=\"\$DB_URL\""
        echo "   dotnet ef database update --project src/domain --startup-project src/api"
        exit 1
    fi
fi

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Migrations completed successfully!"
    echo ""
    echo "🔍 Verifying API health..."
    sleep 2
    curl -s https://observability-dns-api.fly.dev/healthz | jq . || curl -s https://observability-dns-api.fly.dev/healthz
    echo ""
    echo "✅ Done! Your database is now up to date."
else
    echo "❌ Migration failed"
    exit 1
fi
