#!/bin/bash

set -e

echo "🔧 Fixing UI deployment on Fly.io..."
echo ""

# 1. Ensure at least 1 machine is running
echo "📦 Step 1: Scaling to 1 machine..."
fly scale count 1 --app observability-dns-ui

# 2. Set API URL
echo ""
echo "🔗 Step 2: Setting API URL..."
fly secrets set VITE_API_URL=https://observability-dns-api.fly.dev --app observability-dns-ui

# 3. Restart
echo ""
echo "🔄 Step 3: Restarting app..."
fly apps restart observability-dns-ui

# 4. Wait a bit
echo ""
echo "⏳ Step 4: Waiting 15 seconds for machine to start..."
sleep 15

# 5. Check status
echo ""
echo "📊 Step 5: Checking status..."
fly status --app observability-dns-ui

# 6. Test
echo ""
echo "🧪 Step 6: Testing URL..."
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" https://observability-dns-ui.fly.dev || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    echo "✅ UI is responding! (HTTP $HTTP_CODE)"
else
    echo "⚠️  UI returned HTTP $HTTP_CODE"
    echo "   Check logs: fly logs --app observability-dns-ui"
fi

echo ""
echo "✅ Done! Open https://observability-dns-ui.fly.dev in your browser"
echo ""
echo "If still not working, check:"
echo "  - fly logs --app observability-dns-ui"
echo "  - fly machine list --app observability-dns-ui"
