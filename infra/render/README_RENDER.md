# Render Deployment Guide

This guide walks you through deploying the DNS & TLS Observatory to Render.

## Prerequisites

- Render account (sign up at https://render.com)
- GitHub repository with your code
- Slack webhook URL (optional, for alerts)
- SMTP credentials (optional, for email alerts)

## Step 1: Connect Repository

1. Go to Render Dashboard
2. Click "New +" → "Blueprint"
3. Connect your GitHub repository
4. Select the repository containing this project

## Step 2: Configure Blueprint

1. Render will detect `infra/render/render.yaml`
2. Review the service definitions:
   - **API**: Web service (port 8080)
   - **Worker**: Background worker
   - **Database**: PostgreSQL (managed)

## Step 3: Set Environment Variables

### Required Variables

These are automatically set by the Blueprint:
- `ConnectionStrings__DefaultConnection` (from database)
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`

### Optional Variables (Set in Render Dashboard)

For **API** and **Worker** services:

1. **Slack Integration**:
   ```
   SLACK_WEBHOOK_URL=https://hooks.slack.com/services/YOUR/WEBHOOK/URL
   ```

2. **SMTP Email**:
   ```
   SMTP__HOST=smtp.gmail.com
   SMTP__PORT=587
   SMTP__USERNAME=your-email@gmail.com
   SMTP__PASSWORD=your-app-password
   SMTP__FROMEMAIL=your-email@gmail.com
   ```

3. **OpenTelemetry** (if using external collector):
   ```
   OTEL_EXPORTER_OTLP_ENDPOINT=https://your-otel-endpoint.com
   ```

## Step 4: Deploy

1. Click "Apply" in the Blueprint view
2. Render will:
   - Create the PostgreSQL database
   - Build and deploy the API service
   - Build and deploy the Worker service
   - Run database migrations (if configured)

## Step 5: Run Migrations

After first deployment, run migrations:

1. Go to your API service
2. Open the Shell tab
3. Run:
   ```bash
   dotnet ef database update --project src/domain --startup-project src/api
   ```

Or use the migration service (if configured):
```bash
docker compose --profile migrate up db-migrate
```

## Step 6: Verify Deployment

1. **API Health Check**: `https://your-api.onrender.com/healthz`
2. **API Readiness**: `https://your-api.onrender.com/readyz`
3. Check worker logs for probe execution

## Service URLs

After deployment, you'll get:
- **API**: `https://observability-dns-api.onrender.com`
- **Worker**: Runs in background (no public URL)
- **Database**: Internal connection string provided

## Troubleshooting

### Database Connection Issues

- Verify database is created and running
- Check connection string in environment variables
- Ensure database user has proper permissions

### Worker Not Running

- Check worker service logs
- Verify Quartz scheduler configuration
- Ensure database connection is working

### Migration Errors

- Run migrations manually via Shell
- Check database logs
- Verify schema.sql matches migrations

## Cost Estimation

- **Starter Plan**: ~$7/month per service
- **Database**: ~$7/month (starter)
- **Total**: ~$21/month for full stack

## Next Steps

1. Configure custom domain (optional)
2. Set up monitoring/alerts in Render
3. Configure CI/CD for auto-deployments
4. Set up backup schedule for database
