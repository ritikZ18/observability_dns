# Fix 500 Error - Quick Guide

## The Problem

You're getting 500 errors because:
1. **Migration not run** - The `domain_groups` table doesn't exist yet
2. **API not rebuilt** - New GroupsController needs to be compiled

## Quick Fix (Run These Commands)

```bash
# Step 1: Run the migration (creates domain_groups table)
docker compose --profile migrate up db-migrate

# Step 2: Rebuild API with new controllers
docker compose build api

# Step 3: Restart API
docker compose restart api

# Step 4: Verify it works
curl http://localhost:5000/api/groups
```

## What Each Step Does

### Step 1: Run Migration
```bash
docker compose --profile migrate up db-migrate
```
- **Creates** the `domain_groups` table
- **Adds** `group_id` and `icon` columns to `domains` table
- **Sets up** foreign key relationships
- **Without this**: You'll get "relation 'domain_groups' does not exist" error

### Step 2: Rebuild API
```bash
docker compose build api
```
- **Compiles** new code (GroupsController, BackupController)
- **Includes** all latest changes
- **Creates** new Docker image

### Step 3: Restart API
```bash
docker compose restart api
```
- **Starts** API with new code
- **Loads** new controllers
- **Connects** to database

### Step 4: Verify
```bash
curl http://localhost:5000/api/groups
```
- **Should return**: `[]` (empty array) or list of groups
- **If 404**: Migration not run or API not rebuilt
- **If 500**: Check logs: `docker compose logs api --tail 50`

## After Fixing

1. **Refresh your browser** (http://localhost:3000)
2. **Try creating a group** - should work now
3. **Export a backup** - Click "EXPORT BACKUP" button at bottom of dashboard
4. **Save the backup file** - Keep it safe for future restores

## Backup & Restore Feature

The backup feature is now available at the bottom of the dashboard:

- **📥 EXPORT BACKUP**: Downloads a JSON file with all domains and groups
- **📤 IMPORT BACKUP**: Restores domains and groups from a JSON file

### How to Use Backup

1. **Export before changes**:
   - Click "EXPORT BACKUP" 
   - Save the JSON file somewhere safe
   - This file contains all your domains, groups, and configurations

2. **Import after database reset**:
   - Click "IMPORT BACKUP"
   - Select your previously saved JSON file
   - Confirm if you want to clear existing data
   - Your domains and groups will be restored

### What's Backed Up

- ✅ All groups (name, description, color, icon)
- ✅ All domains (name, interval, icon, group assignment)
- ✅ All checks (DNS, TLS, HTTP configurations)
- ❌ Probe runs (not included - these are monitoring data)
- ❌ Incidents (not included - these are historical data)

## Verify Migration Worked

```bash
# Check if domain_groups table exists
docker compose exec postgres psql -U observability -d observability_dns -c "\d domain_groups"

# Check if domains table has group_id column
docker compose exec postgres psql -U observability -d observability_dns -c "\d domains" | grep group_id

# List all tables
docker compose exec postgres psql -U observability -d observability_dns -c "\dt"
```

## Troubleshooting

### Still getting 500 error?

1. **Check API logs**:
   ```bash
   docker compose logs api --tail 100 | grep -i error
   ```

2. **Verify migration ran**:
   ```bash
   docker compose logs db-migrate
   ```

3. **Check database connection**:
   ```bash
   docker compose exec postgres psql -U observability -d observability_dns -c "SELECT version();"
   ```

4. **Restart everything**:
   ```bash
   docker compose down
   docker compose up -d postgres
   sleep 5
   docker compose --profile migrate up db-migrate
   docker compose up -d
   ```

### Migration fails?

1. **Check migration files exist**:
   ```bash
   ls -la src/domain/Migrations/
   ```

2. **Run migration manually**:
   ```bash
   docker compose exec api dotnet ef database update --project ../domain --startup-project .
   ```

## Next Steps After Fix

1. ✅ **Run the 4 steps above**
2. ✅ **Test creating a group**
3. ✅ **Export a backup** (saves your data)
4. ✅ **Keep backup file safe** (for future restores)

Now you won't lose your data when restarting the database! 🎉
