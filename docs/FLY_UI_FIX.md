# Fixing UI "Suspended" and "Proxy not finding machines" Issue

## Problem

UI shows:
- Black screen
- "Proxy not finding machines to route requests"
- "Suspended" status

## Causes

1. **Machine is suspended** - Free tier machines auto-suspend after inactivity
2. **Machine not starting** - Health checks failing or configuration issue
3. **No machines running** - All machines stopped

## Solutions

### Solution 1: Wake Up the Machine

```bash
# Check machine status
fly status --app observability-dns-ui

# Wake up the machine (make a request)
curl https://observability-dns-ui.fly.dev

# Or restart the machine
fly apps restart observability-dns-ui

# Or scale to ensure at least 1 machine is running
fly scale count 1 --app observability-dns-ui
```

### Solution 2: Check Machine Logs

```bash
# View logs to see what's wrong
fly logs --app observability-dns-ui

# Check for errors
fly logs --app observability-dns-ui | grep -i error
```

### Solution 3: Update fly.ui.toml Configuration

The UI might be configured to auto-stop. Update `fly.ui.toml`:

```toml
[http_service]
  internal_port = 80
  force_https = true
  auto_stop_machines = false  # Changed from true
  auto_start_machines = true
  min_machines_running = 1    # Changed from 0
  processes = ["app"]
```

Then redeploy:
```bash
fly deploy --config fly.ui.toml --app observability-dns-ui
```

### Solution 4: Check if Machine is Actually Running

```bash
# List all machines
fly machine list --app observability-dns-ui

# If no machines or all stopped, create one
fly machine start --app observability-dns-ui

# Or scale up
fly scale count 1 --app observability-dns-ui
```

### Solution 5: Verify Nginx Configuration

The UI uses Nginx. Check if it's configured correctly:

```bash
# SSH into UI container
fly ssh console --app observability-dns-ui

# Check if nginx is running
ps aux | grep nginx

# Check nginx config
cat /etc/nginx/conf.d/default.conf

# Check if files exist
ls -la /usr/share/nginx/html/
```

### Solution 6: Rebuild and Redeploy

If nothing works, rebuild:

```bash
# Set API URL
fly secrets set VITE_API_URL=https://observability-dns-api.fly.dev --app observability-dns-ui

# Rebuild and deploy
fly deploy --config fly.ui.toml --app observability-dns-ui --build-only
fly deploy --config fly.ui.toml --app observability-dns-ui
```

---

## Quick Fix Commands

```bash
# 1. Check status
fly status --app observability-dns-ui

# 2. Wake up machine
fly scale count 1 --app observability-dns-ui

# 3. Restart
fly apps restart observability-dns-ui

# 4. Check logs
fly logs --app observability-dns-ui

# 5. Test URL
curl -I https://observability-dns-ui.fly.dev
```

---

## Common Issues

### Issue: "No machines found"

**Fix:**
```bash
fly scale count 1 --app observability-dns-ui
```

### Issue: "Machine suspended"

**Fix:**
```bash
# Make a request to wake it up
curl https://observability-dns-ui.fly.dev

# Or restart
fly apps restart observability-dns-ui
```

### Issue: "502 Bad Gateway"

**Fix:**
- Check if nginx is running inside container
- Check if static files exist in `/usr/share/nginx/html/`
- Check nginx configuration

### Issue: "Black screen / Blank page"

**Possible causes:**
1. API URL not set correctly
2. CORS issues
3. JavaScript errors

**Fix:**
```bash
# Set API URL
fly secrets set VITE_API_URL=https://observability-dns-api.fly.dev --app observability-dns-ui

# Check browser console for errors
# Open: https://observability-dns-ui.fly.dev
# Press F12 → Console tab
```

---

## Verification Steps

After fixing, verify:

1. **Machine is running:**
   ```bash
   fly status --app observability-dns-ui
   ```

2. **UI responds:**
   ```bash
   curl -I https://observability-dns-ui.fly.dev
   # Should return 200 OK
   ```

3. **Browser loads:**
   - Open https://observability-dns-ui.fly.dev
   - Check browser console (F12) for errors
   - Check Network tab for API calls

---

## Complete Fix Script

```bash
#!/bin/bash

echo "🔧 Fixing UI deployment..."

# 1. Ensure at least 1 machine is running
echo "📦 Scaling to 1 machine..."
fly scale count 1 --app observability-dns-ui

# 2. Set API URL
echo "🔗 Setting API URL..."
fly secrets set VITE_API_URL=https://observability-dns-api.fly.dev --app observability-dns-ui

# 3. Restart
echo "🔄 Restarting app..."
fly apps restart observability-dns-ui

# 4. Wait a bit
echo "⏳ Waiting 10 seconds..."
sleep 10

# 5. Check status
echo "📊 Checking status..."
fly status --app observability-dns-ui

# 6. Test
echo "🧪 Testing URL..."
curl -I https://observability-dns-ui.fly.dev

echo ""
echo "✅ Done! Open https://observability-dns-ui.fly.dev in your browser"
```
