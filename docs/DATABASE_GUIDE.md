# Database Guide - Viewing and Managing Data

## What the Commands Do

### 1. Run Database Migration
```bash
docker compose --profile migrate up db-migrate
```

**What it does:**
- Runs the EF Core migration `AddDomainGroupsAndIcons`
- Creates the `domain_groups` table in PostgreSQL
- Adds `group_id` and `icon` columns to the `domains` table
- Sets up foreign key relationships
- **Important:** Must be run before creating groups in the UI

**When to run:**
- First time setup
- After pulling new migrations
- If you see 404 errors when creating groups

### 2. Rebuild Containers
```bash
docker compose up -d --build
```

**What it does:**
- Rebuilds Docker images with latest code changes
- Includes new API endpoints (GroupsController)
- Updates UI with new components (GroupForm, GroupDetail)
- Restarts all services with new code
- **Important:** Required after adding new API controllers or UI components

**When to run:**
- After code changes
- When API endpoints return 404
- After adding new dependencies

## How to View the Database

### Option 1: Using Docker Exec (Recommended)

#### Connect to PostgreSQL Container
```bash
docker compose exec postgres psql -U observability -d observability_dns
```

#### View All Tables
```sql
\dt
```

#### View Table Schemas
```sql
\d domains
\d domain_groups
\d checks
\d probe_runs
\d incidents
```

#### View All Domains
```sql
SELECT id, name, enabled, interval_minutes, icon, group_id, created_at 
FROM domains 
ORDER BY created_at DESC;
```

#### View All Groups
```sql
SELECT id, name, description, color, icon, enabled, created_at 
FROM domain_groups 
ORDER BY created_at DESC;
```

#### View Domains with Their Groups
```sql
SELECT 
    d.id,
    d.name as domain_name,
    d.icon as domain_icon,
    d.enabled as domain_enabled,
    g.name as group_name,
    g.color as group_color,
    g.icon as group_icon
FROM domains d
LEFT JOIN domain_groups g ON d.group_id = g.id
ORDER BY g.name NULLS LAST, d.name;
```

#### View Recent Probe Runs
```sql
SELECT 
    pr.id,
    d.name as domain_name,
    pr.check_type,
    pr.success,
    pr.total_ms,
    pr.completed_at
FROM probe_runs pr
JOIN domains d ON pr.domain_id = d.id
ORDER BY pr.completed_at DESC
LIMIT 20;
```

#### View Group Statistics
```sql
SELECT 
    g.name as group_name,
    COUNT(DISTINCT d.id) as total_domains,
    COUNT(DISTINCT CASE WHEN d.enabled THEN d.id END) as enabled_domains,
    COUNT(pr.id) as total_probe_runs,
    COUNT(CASE WHEN pr.success THEN 1 END) as successful_runs,
    COUNT(CASE WHEN NOT pr.success THEN 1 END) as failed_runs,
    ROUND(AVG(pr.total_ms)::numeric, 2) as avg_latency_ms
FROM domain_groups g
LEFT JOIN domains d ON g.id = d.group_id
LEFT JOIN probe_runs pr ON d.id = pr.domain_id
GROUP BY g.id, g.name
ORDER BY g.name;
```

#### Count Records in Each Table
```sql
SELECT 
    'domains' as table_name, COUNT(*) as count FROM domains
UNION ALL
SELECT 'domain_groups', COUNT(*) FROM domain_groups
UNION ALL
SELECT 'checks', COUNT(*) FROM checks
UNION ALL
SELECT 'probe_runs', COUNT(*) FROM probe_runs
UNION ALL
SELECT 'incidents', COUNT(*) FROM incidents
UNION ALL
SELECT 'alert_rules', COUNT(*) FROM alert_rules
UNION ALL
SELECT 'notifications', COUNT(*) FROM notifications;
```

#### Exit PostgreSQL
```sql
\q
```

### Option 2: Using pgAdmin (GUI Tool)

#### Install pgAdmin
```bash
# Download from https://www.pgadmin.org/download/
# Or use Docker:
docker run -d \
  --name pgadmin \
  -p 5050:80 \
  -e PGADMIN_DEFAULT_EMAIL=admin@example.com \
  -e PGADMIN_DEFAULT_PASSWORD=admin \
  dpage/pgadmin4
```

#### Connect to Database
1. Open http://localhost:5050
2. Login with email: `admin@example.com`, password: `admin`
3. Add new server:
   - **Name:** Observability DNS
   - **Host:** `postgres` (or `localhost` if connecting from host)
   - **Port:** `5432`
   - **Username:** `observability`
   - **Password:** `observability_dev`
   - **Database:** `observability_dns`

### Option 3: Using psql from Host Machine

#### Install PostgreSQL Client (if not installed)
```bash
# Ubuntu/Debian
sudo apt-get install postgresql-client

# macOS
brew install postgresql

# Windows
# Download from https://www.postgresql.org/download/windows/
```

#### Connect
```bash
psql -h localhost -p 5432 -U observability -d observability_dns
# Password: observability_dev
```

### Option 4: Export Database to SQL File

#### Export Entire Database
```bash
docker compose exec postgres pg_dump -U observability observability_dns > backup.sql
```

#### Export Specific Table
```bash
docker compose exec postgres pg_dump -U observability -t domains observability_dns > domains.sql
docker compose exec postgres pg_dump -U observability -t domain_groups observability_dns > groups.sql
```

## Common Database Queries

### Check if Migration Was Applied
```sql
-- Check if domain_groups table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_name = 'domain_groups'
);

-- Check if domains table has group_id column
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'domains' AND column_name = 'group_id';
```

### Find Domains Without Groups
```sql
SELECT id, name, enabled 
FROM domains 
WHERE group_id IS NULL;
```

### Find Groups Without Domains
```sql
SELECT id, name, color 
FROM domain_groups g
WHERE NOT EXISTS (
    SELECT 1 FROM domains d WHERE d.group_id = g.id
);
```

### View Recent Activity
```sql
SELECT 
    d.name as domain,
    pr.check_type,
    pr.success,
    pr.total_ms || 'ms' as latency,
    pr.completed_at
FROM probe_runs pr
JOIN domains d ON pr.domain_id = d.id
ORDER BY pr.completed_at DESC
LIMIT 50;
```

### Get Domain Health Summary
```sql
SELECT 
    d.name,
    d.enabled,
    g.name as group_name,
    COUNT(pr.id) as total_checks,
    COUNT(CASE WHEN pr.success THEN 1 END) as successful,
    COUNT(CASE WHEN NOT pr.success THEN 1 END) as failed,
    ROUND(AVG(pr.total_ms)::numeric, 2) as avg_latency,
    MAX(pr.completed_at) as last_check
FROM domains d
LEFT JOIN domain_groups g ON d.group_id = g.id
LEFT JOIN probe_runs pr ON d.id = pr.domain_id
GROUP BY d.id, d.name, d.enabled, g.name
ORDER BY d.name;
```

## Troubleshooting

### Fix 404 Error When Creating Groups

**Problem:** API returns 404 when creating groups

**Solution:**
1. **Rebuild API container** (includes new GroupsController):
   ```bash
   docker compose build api
   docker compose up -d api
   ```

2. **Run migration** (creates domain_groups table):
   ```bash
   docker compose --profile migrate up db-migrate
   ```

3. **Verify migration**:
   ```bash
   docker compose exec postgres psql -U observability -d observability_dns -c "\dt"
   # Should show domain_groups table
   ```

4. **Check API logs**:
   ```bash
   docker compose logs api --tail 50
   ```

### Reset Database (⚠️ Deletes All Data)

```bash
# Stop containers
docker compose down

# Remove volumes (deletes all data)
docker compose down -v

# Start fresh
docker compose up -d postgres
docker compose --profile migrate up db-migrate
docker compose up -d
```

### Check Database Connection

```bash
# Test connection from host
docker compose exec postgres psql -U observability -d observability_dns -c "SELECT version();"

# Test connection from API container
docker compose exec api dotnet ef database update --project ../domain --startup-project . --dry-run
```

## Quick Reference

```bash
# View all tables
docker compose exec postgres psql -U observability -d observability_dns -c "\dt"

# Count records
docker compose exec postgres psql -U observability -d observability_dns -c "SELECT COUNT(*) FROM domains;"

# View domains
docker compose exec postgres psql -U observability -d observability_dns -c "SELECT * FROM domains;"

# View groups
docker compose exec postgres psql -U observability -d observability_dns -c "SELECT * FROM domain_groups;"

# View recent probe runs
docker compose exec postgres psql -U observability -d observability_dns -c "SELECT * FROM probe_runs ORDER BY completed_at DESC LIMIT 10;"
```
