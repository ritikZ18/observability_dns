# Deployment Guide

This guide covers deploying the DNS & TLS Observatory to production environments.

## Deployment Options

1. **Render** (Recommended for MVP) - Easiest PaaS deployment
2. **Azure** - Good for .NET ecosystem
3. **AWS** - Full control, more complex
4. **Self-Hosted** - VPS or dedicated server

## Render Deployment

See [infra/render/README_RENDER.md](../infra/render/README_RENDER.md) for detailed Render deployment instructions.

### Quick Render Deploy

1. Push code to GitHub
2. Connect repository to Render
3. Use Blueprint: `infra/render/render.yaml`
4. Set environment variables
5. Deploy

## Azure Deployment

### Prerequisites

- Azure account
- Azure CLI installed
- .NET 8 SDK

### Steps

1. **Create Resource Group**:
   ```bash
   az group create --name observability-dns-rg --location eastus
   ```

2. **Create PostgreSQL Database**:
   ```bash
   az postgres flexible-server create \
     --resource-group observability-dns-rg \
     --name observability-dns-db \
     --admin-user observability \
     --admin-password <secure-password> \
     --sku-name Standard_B1ms \
     --tier Burstable
   ```

3. **Create App Service Plan**:
   ```bash
   az appservice plan create \
     --name observability-dns-plan \
     --resource-group observability-dns-rg \
     --sku B1 \
     --is-linux
   ```

4. **Create API Web App**:
   ```bash
   az webapp create \
     --name observability-dns-api \
     --resource-group observability-dns-rg \
     --plan observability-dns-plan \
     --runtime "DOTNET|8.0"
   ```

5. **Create Worker Web App** (as background job):
   ```bash
   az webapp create \
     --name observability-dns-worker \
     --resource-group observability-dns-rg \
     --plan observability-dns-plan \
     --runtime "DOTNET|8.0"
   ```

6. **Configure Connection Strings**:
   ```bash
   az webapp config connection-string set \
     --name observability-dns-api \
     --resource-group observability-dns-rg \
     --settings DefaultConnection="<connection-string>" \
     --connection-string-type PostgreSQL
   ```

7. **Deploy**:
   ```bash
   cd src/api
   az webapp up --name observability-dns-api --resource-group observability-dns-rg
   ```

## AWS Deployment

### Using ECS/Fargate

1. **Build Docker Images**:
   ```bash
   docker build -t observability-dns-api -f src/api/Dockerfile .
   docker build -t observability-dns-worker -f src/worker/Dockerfile .
   ```

2. **Push to ECR**:
   ```bash
   aws ecr create-repository --repository-name observability-dns-api
   docker tag observability-dns-api:latest <account>.dkr.ecr.<region>.amazonaws.com/observability-dns-api:latest
   docker push <account>.dkr.ecr.<region>.amazonaws.com/observability-dns-api:latest
   ```

3. **Create RDS PostgreSQL**:
   ```bash
   aws rds create-db-instance \
     --db-instance-identifier observability-dns-db \
     --db-instance-class db.t3.micro \
     --engine postgres \
     --master-username observability \
     --master-user-password <secure-password> \
     --allocated-storage 20
   ```

4. **Create ECS Task Definitions** for API and Worker

5. **Create ECS Services** with Fargate launch type

## Self-Hosted Deployment

### VPS Setup (Ubuntu/Debian)

1. **Install Dependencies**:
   ```bash
   sudo apt update
   sudo apt install -y docker.io docker-compose postgresql-client
   ```

2. **Clone Repository**:
   ```bash
   git clone <repository-url>
   cd observability_dns
   ```

3. **Configure Environment**:
   ```bash
   cp .env.example .env
   # Edit .env with production values
   ```

4. **Start Services**:
   ```bash
   docker compose -f docker-compose.prod.yml up -d
   ```

5. **Run Migrations**:
   ```bash
   docker compose exec api dotnet ef database update --project src/domain --startup-project src/api
   ```

6. **Set Up Reverse Proxy** (Nginx):
   ```nginx
   server {
       listen 80;
       server_name your-domain.com;
       
       location / {
           proxy_pass http://localhost:5000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
   }
   ```

## Environment Variables (Production)

### Required

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<production-connection-string>
```

### Optional

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=<otel-endpoint>
SLACK_WEBHOOK_URL=<slack-webhook>
SMTP__HOST=<smtp-host>
SMTP__PORT=587
SMTP__USERNAME=<smtp-username>
SMTP__PASSWORD=<smtp-password>
SMTP__FROMEMAIL=<from-email>
```

## Database Migrations

Always run migrations before starting services:

```bash
# Using EF Core
dotnet ef database update --project src/domain --startup-project src/api

# Or using Docker
docker compose run --rm db-migrate
```

## Health Checks

Verify deployment:

```bash
# API Health
curl https://your-api.com/healthz
curl https://your-api.com/readyz

# Should return 200 OK
```

## Monitoring Production

1. **Application Logs**: Check service logs regularly
2. **Database**: Monitor connection pool and query performance
3. **Worker**: Verify probe execution in logs
4. **Alerts**: Test Slack/Email notifications

## Backup Strategy

### Database Backups

```bash
# Manual backup
pg_dump -h <host> -U observability observability_dns > backup.sql

# Automated (cron)
0 2 * * * pg_dump -h <host> -U observability observability_dns > /backups/db-$(date +\%Y\%m\%d).sql
```

### Render Backups

Render automatically backs up managed PostgreSQL databases. Configure backup schedule in Render dashboard.

## Scaling

### Horizontal Scaling

- **API**: Deploy multiple instances behind load balancer
- **Worker**: Run multiple worker instances (they coordinate via database)

### Vertical Scaling

- Increase database instance size
- Upgrade App Service Plan (Azure)
- Increase ECS task CPU/memory (AWS)

## Security Checklist

- [ ] Use strong database passwords
- [ ] Enable HTTPS (TLS certificates)
- [ ] Set up firewall rules
- [ ] Rotate credentials regularly
- [ ] Enable database encryption at rest
- [ ] Use secrets management (Azure Key Vault, AWS Secrets Manager)
- [ ] Enable API authentication (future)

## Troubleshooting Production

### API Not Responding

1. Check service logs
2. Verify database connectivity
3. Check health endpoints
4. Review resource usage (CPU/memory)

### Worker Not Running

1. Check worker logs
2. Verify Quartz scheduler configuration
3. Check database connection
4. Review probe execution errors

### Database Issues

1. Check connection pool limits
2. Monitor slow queries
3. Review database logs
4. Verify backup/restore procedures

## Rollback Procedure

1. **Code Rollback**:
   ```bash
   git revert <commit-hash>
   git push
   # Redeploy
   ```

2. **Database Rollback**:
   ```bash
   dotnet ef database update <previous-migration> --project src/domain --startup-project src/api
   ```

3. **Container Rollback**:
   ```bash
   docker compose down
   docker compose up -d <previous-image-tag>
   ```
