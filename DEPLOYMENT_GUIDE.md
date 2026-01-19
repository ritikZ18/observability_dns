# Deployment Guide - Free & Local Hosting

## 🎯 Quick Answer

### Best Free Options:
1. **Render.com** (Recommended) - Free tier with PostgreSQL
2. **Railway.app** - $5 credit/month (effectively free)
3. **Fly.io** - Free tier with limited resources
4. **Self-hosted** - Local server/VPS (completely free)

---

## 🚀 Option 1: Render.com (Recommended - Easiest)

**Pros:**
- ✅ Free PostgreSQL database
- ✅ Free web services (with limits)
- ✅ Automatic HTTPS
- ✅ Easy setup with Render Blueprint
- ✅ GitHub integration

**Cons:**
- ❌ Free tier spins down after 15 minutes of inactivity
- ❌ Limited resources

### Step-by-Step:

1. **Push to GitHub**:
   ```bash
   git add .
   git commit -m "Fix TypeScript errors and prepare for deployment"
   git push origin main
   ```

2. **Sign up at Render.com**: https://render.com

3. **Create New Blueprint**:
   - Click "New +" → "Blueprint"
   - Connect your GitHub repository
   - Render will detect `infra/render/render.yaml`

4. **Review Services** (will be auto-created):
   - PostgreSQL Database (free)
   - Web Service (API)
   - Background Worker (Worker Service)
   - Static Site (UI)

5. **Deploy**: Click "Apply" - Render will deploy everything!

**Note:** Render Blueprint is already configured at `infra/render/render.yaml`

---

## 🚂 Option 2: Railway.app

**Pros:**
- ✅ $5 credit/month (effectively free for small apps)
- ✅ PostgreSQL included
- ✅ No spin-down
- ✅ Easy setup

**Cons:**
- ❌ Credit expires after 30 days
- ❌ Need to provide payment method (but free tier available)

### Step-by-Step:

1. **Install Railway CLI**:
   ```bash
   npm install -g @railway/cli
   railway login
   ```

2. **Initialize Project**:
   ```bash
   railway init
   railway up
   ```

3. **Add PostgreSQL**:
   ```bash
   railway add postgresql
   ```

4. **Set Environment Variables**:
   ```bash
   railway variables set ConnectionStrings__DefaultConnection="$(railway variables | grep DATABASE_URL)"
   ```

5. **Deploy**:
   ```bash
   railway up
   ```

---

## ✈️ Option 3: Fly.io

**Pros:**
- ✅ Free tier available
- ✅ Global edge locations
- ✅ PostgreSQL included

**Cons:**
- ❌ More complex setup
- ❌ Limited free resources

### Step-by-Step:

1. **Install Fly CLI**:
   ```bash
   curl -L https://fly.io/install.sh | sh
   fly auth login
   ```

2. **Create Apps**:
   ```bash
   fly apps create observability-dns-api
   fly apps create observability-dns-worker
   fly apps create observability-dns-ui
   ```

3. **Add PostgreSQL**:
   ```bash
   fly postgres create observability-dns-db
   ```

4. **Deploy** (see Fly.io docs for full setup)

---

## 🏠 Option 4: Local Deployment (Self-Hosted)

**Pros:**
- ✅ Completely free
- ✅ Full control
- ✅ No limits

**Cons:**
- ❌ Need a server/VPS
- ❌ Need to manage updates
- ❌ Need to configure DNS/SSL

### Option 4A: Docker Compose (Easiest Local)

Already configured! Just run:

```bash
# Start all services
docker compose up -d

# Access:
# - UI: http://localhost:3000
# - API: http://localhost:5000
# - PostgreSQL: localhost:5432
```

### Option 4B: VPS (e.g., DigitalOcean, Linode)

1. **Rent a VPS** ($5-10/month):
   - DigitalOcean Droplet
   - Linode Nanode
   - Vultr VPS

2. **Install Docker**:
   ```bash
   curl -fsSL https://get.docker.com -o get-docker.sh
   sh get-docker.sh
   sudo usermod -aG docker $USER
   ```

3. **Clone Repository**:
   ```bash
   git clone <your-repo-url>
   cd observability_dns
   ```

4. **Set Up Environment**:
   ```bash
   # Create .env file
   cat > .env << EOF
   ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=observability_dns;Username=observability;Password=YOUR_SECURE_PASSWORD
   ASPNETCORE_ENVIRONMENT=Production
   EOF
   ```

5. **Deploy**:
   ```bash
   # Run migration
   docker compose --profile migrate up db-migrate
   
   # Start services
   docker compose up -d
   ```

6. **Set Up Reverse Proxy (Nginx)**:
   ```nginx
   server {
       listen 80;
       server_name your-domain.com;
       
       location / {
           proxy_pass http://localhost:3000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
       
       location /api {
           proxy_pass http://localhost:5000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
   }
   ```

7. **Add SSL (Let's Encrypt)**:
   ```bash
   sudo apt install certbot python3-certbot-nginx
   sudo certbot --nginx -d your-domain.com
   ```

---

## 📋 Deployment Checklist

### Before Deploying:

- [ ] Fix all TypeScript errors ✅ (Done!)
- [ ] Test locally with `docker compose up`
- [ ] Export backup of current data (if any)
- [ ] Set environment variables
- [ ] Run database migrations
- [ ] Configure CORS (if needed)
- [ ] Set up SSL/HTTPS

### Environment Variables Needed:

```bash
# Database
ConnectionStrings__DefaultConnection=<connection-string>

# API
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# OpenTelemetry (optional)
OTEL_EXPORTER_OTLP_ENDPOINT=<endpoint>

# Notifications (optional)
SLACK_WEBHOOK_URL=<url>
SMTP__HOST=<host>
SMTP__PORT=587
SMTP__USERNAME=<email>
SMTP__PASSWORD=<password>
```

---

## 🔧 Render.com Specific Setup

### 1. Update `infra/render/render.yaml`:

The file already exists at `infra/render/render.yaml`. It includes:
- PostgreSQL service
- API web service
- Worker background service
- UI static site

### 2. GitHub Integration:

1. Push code to GitHub
2. Connect GitHub to Render
3. Render will auto-detect `render.yaml`
4. Review services and click "Apply"

### 3. Environment Variables in Render:

Set these in Render dashboard:
- `ConnectionStrings__DefaultConnection` (auto-set from PostgreSQL)
- `ASPNETCORE_ENVIRONMENT=Production`
- `OTEL_EXPORTER_OTLP_ENDPOINT` (optional)

---

## 📊 Comparison Table

| Option | Cost | Setup Difficulty | Best For |
|--------|------|------------------|----------|
| **Render** | Free | ⭐ Easy | Quick deployment, demos |
| **Railway** | $5/mo credit | ⭐⭐ Medium | Production-ready |
| **Fly.io** | Free tier | ⭐⭐⭐ Hard | Global edge locations |
| **Local/VPS** | Free/$5-10/mo | ⭐⭐⭐⭐ Very Hard | Full control |

---

## 🚨 Important Notes

### Render Free Tier Limitations:
- Services spin down after 15 minutes of inactivity
- First request after spin-down takes 30-60 seconds
- Limited to 512MB RAM per service
- PostgreSQL has 90-day retention limit

### Railway Limitations:
- $5 credit/month
- Need credit card (but won't be charged on free tier)
- Credit expires after 30 days

### Self-Hosted Considerations:
- Need to manage updates manually
- Need to configure firewall
- Need to set up monitoring
- Need backup strategy

---

## ✅ Recommended Path

**For Quick Demo/Testing:**
→ Use **Render.com** (free, easy, 5 minutes setup)

**For Production:**
→ Use **Railway.app** or **Self-hosted VPS** ($5-10/month)

**For Development:**
→ Use **Local Docker Compose** (completely free)

---

## 📝 Quick Start (Render)

```bash
# 1. Push to GitHub
git add .
git commit -m "Ready for deployment"
git push origin main

# 2. Go to Render.com
# 3. Click "New +" → "Blueprint"
# 4. Connect GitHub repo
# 5. Click "Apply"
# Done! 🎉
```

Your services will be live at:
- UI: `https://your-app-name.onrender.com`
- API: `https://your-api-name.onrender.com`

---

## 🔍 Monitoring After Deployment

After deployment, check:
- ✅ API health: `https://your-api.onrender.com/healthz`
- ✅ Database connection
- ✅ Worker service logs
- ✅ UI loads correctly

---

## 📚 Additional Resources

- [Render Docs](https://render.com/docs)
- [Railway Docs](https://docs.railway.app)
- [Fly.io Docs](https://fly.io/docs)
- [Docker Compose Docs](https://docs.docker.com/compose/)

---

## 🆘 Troubleshooting

### CI/CD Failed?

The CI/CD workflow checks:
- ✅ .NET builds
- ✅ TypeScript builds (now fixed!)
- ✅ Database migrations

**If it fails:**
```bash
# Test locally first
cd ui && npm run build
dotnet build src/api/ObservabilityDns.Api.csproj
```

### Deployment Issues?

1. **Check logs** in Render/Railway dashboard
2. **Verify environment variables** are set
3. **Check database connection** string
4. **Run migrations** manually if needed

---

## 💡 Pro Tips

1. **Use Render for demos** - fastest setup
2. **Use Railway for production** - more reliable
3. **Backup before deploying** - use export feature
4. **Set up monitoring** - use health check endpoints
5. **Enable auto-deploy** - push to GitHub → auto-deploy

---

Good luck with your deployment! 🚀
