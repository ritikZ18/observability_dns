# Quick Fix for 500 Error

## The Problem

You're getting this error:
```
column "group_id" of relation "domains" does not exist
```

**Why?** The database migration hasn't been run yet. The `domain_groups` table and `group_id`/`icon` columns don't exist in your database.

## The Solution (3 Simple Steps)

### Step 1: Run the Migration
```bash
docker compose --profile migrate up db-migrate
```

This creates:
- ✅ `domain_groups` table
- ✅ `group_id` column in `domains` table
- ✅ `icon` column in `domains` table
- ✅ Foreign key relationships

### Step 2: Verify Migration Worked
```bash
# Check if domain_groups table exists
docker compose exec postgres psql -U observability -d observability_dns -c "\d domain_groups"

# Check if domains table has group_id column
docker compose exec postgres psql -U observability -d observability_dns -c "\d domains" | grep group_id
```

### Step 3: Restart API
```bash
docker compose restart api
```

## After Running These Steps

1. **Refresh your browser** (http://localhost:3000)
2. **Try adding a domain** - should work now! ✅
3. **Try creating a group** - should work now! ✅

## What Each Step Does

### Step 1: Migration
- Creates the database schema changes
- Adds new tables and columns
- Sets up relationships
- **Required before** you can use groups or icons

### Step 2: Verification
- Confirms tables were created
- Makes sure columns exist
- Helps debug if something went wrong

### Step 3: Restart API
- Reloads the API with correct schema
- Connects to updated database
- Makes endpoints available

## Troubleshooting

### Migration fails?

Check if migration file exists:
```bash
ls -la src/domain/Migrations/
```

You should see a migration file like:
```
AddDomainGroupsAndIcons.cs
```

### Still getting 500 error?

1. **Check API logs**:
   ```bash
   docker compose logs api --tail 50 | grep -i error
   ```

2. **Verify database connection**:
   ```bash
   docker compose exec postgres psql -U observability -d observability_dns -c "SELECT version();"
   ```

3. **Check migration ran**:
   ```bash
   docker compose logs db-migrate
   ```

### Need to start fresh?

If you want to start completely fresh:
```bash
# Stop everything
docker compose down

# Remove volumes (⚠️ deletes all data)
docker compose down -v

# Start database
docker compose up -d postgres

# Wait a few seconds, then run migration
sleep 5
docker compose --profile migrate up db-migrate

# Start everything
docker compose up -d
```

## Summary

**The issue:** Migration not run → Missing database columns  
**The fix:** Run migration → Restart API → Done!

```bash
docker compose --profile migrate up db-migrate
docker compose restart api
```

That's it! After these two commands, everything should work. 🎉
