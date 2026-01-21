# Scripts

Utility scripts for development, deployment, and maintenance.

## Structure

```
scripts/
├── deployment/     # Deployment scripts (Fly.io, etc.)
├── development/    # Development utilities
└── maintenance/    # Maintenance tasks
```

## Deployment Scripts

### `deployment/deploy-fly.sh`
Complete deployment script for Fly.io. Deploys API, Worker, UI, and runs migrations.

**Usage**:
```bash
./scripts/deployment/deploy-fly.sh
```

### `deployment/run-fly-migrations.sh`
Run database migrations on Fly.io deployment.

**Usage**:
```bash
./scripts/deployment/run-fly-migrations.sh
```

### `deployment/fix-fly-ui.sh`
Fix UI suspension issues on Fly.io.

**Usage**:
```bash
./scripts/deployment/fix-fly-ui.sh
```

## Development Scripts

### `development/setup.sh`
Initial project setup script.

### `development/reset-db.sh`
Reset local database for development.

## Maintenance Scripts

### `maintenance/backup.sh`
Backup database and configuration.

### `maintenance/cleanup.sh`
Clean up temporary files and build artifacts.
