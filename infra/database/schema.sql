-- Bootstrap schema for Docker initialization
-- This is the initial schema. Future changes should use EF Core migrations

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Domains table
CREATE TABLE IF NOT EXISTS domains (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL UNIQUE,
    enabled BOOLEAN NOT NULL DEFAULT true,
    interval_minutes INTEGER NOT NULL DEFAULT 5 CHECK (interval_minutes IN (1, 5, 15)),
    icon VARCHAR(50),
    group_id UUID,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Domain groups table (optional grouping for domains)
CREATE TABLE IF NOT EXISTS domain_groups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(500),
    color VARCHAR(7),
    icon VARCHAR(50),
    enabled BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Foreign key from domains -> domain_groups (optional)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_domains_domain_groups_group_id'
    ) THEN
        ALTER TABLE domains
            ADD CONSTRAINT "FK_domains_domain_groups_group_id"
            FOREIGN KEY (group_id) REFERENCES domain_groups(id) ON DELETE SET NULL;
    END IF;
END $$;

-- Checks table (defines what checks to run for each domain)
CREATE TABLE IF NOT EXISTS checks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    domain_id UUID NOT NULL REFERENCES domains(id) ON DELETE CASCADE,
    check_type VARCHAR(50) NOT NULL CHECK (check_type IN ('DNS', 'TLS', 'HTTP')),
    enabled BOOLEAN NOT NULL DEFAULT true,
    configuration JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(domain_id, check_type)
);

-- Probe runs table (stores results of each probe execution)
CREATE TABLE IF NOT EXISTS probe_runs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    domain_id UUID NOT NULL REFERENCES domains(id) ON DELETE CASCADE,
    check_id UUID NOT NULL REFERENCES checks(id) ON DELETE CASCADE,
    check_type VARCHAR(50) NOT NULL,
    success BOOLEAN NOT NULL,
    error_code VARCHAR(100),
    error_message TEXT,
    dns_ms INTEGER,
    tls_ms INTEGER,
    ttfb_ms INTEGER,
    total_ms INTEGER NOT NULL,
    status_code INTEGER,
    records_snapshot JSONB,
    certificate_info JSONB,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Incidents table (tracks open/closed incidents)
CREATE TABLE IF NOT EXISTS incidents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    domain_id UUID NOT NULL REFERENCES domains(id) ON DELETE CASCADE,
    check_type VARCHAR(50) NOT NULL,
    severity VARCHAR(20) NOT NULL CHECK (severity IN ('LOW', 'MEDIUM', 'HIGH', 'CRITICAL')),
    status VARCHAR(20) NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN', 'RESOLVED', 'ACKNOWLEDGED')),
    reason TEXT,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Alert rules table (defines when to trigger alerts)
CREATE TABLE IF NOT EXISTS alert_rules (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    domain_id UUID NOT NULL REFERENCES domains(id) ON DELETE CASCADE,
    check_type VARCHAR(50) NOT NULL,
    trigger_condition VARCHAR(100) NOT NULL, -- e.g., "3 fails in 5 runs"
    enabled BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Notifications table (outbox pattern for reliable alert delivery)
CREATE TABLE IF NOT EXISTS notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    domain_id UUID NOT NULL REFERENCES domains(id) ON DELETE CASCADE,
    incident_id UUID REFERENCES incidents(id) ON DELETE SET NULL,
    channel VARCHAR(50) NOT NULL CHECK (channel IN ('SLACK', 'EMAIL')),
    destination VARCHAR(500) NOT NULL, -- webhook URL or email address
    payload JSONB NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'SENT', 'FAILED')),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP WITH TIME ZONE,
    retry_count INTEGER NOT NULL DEFAULT 0
);

-- Notification attempts table (tracks retry attempts)
CREATE TABLE IF NOT EXISTS notification_attempts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    notification_id UUID NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
    attempt_number INTEGER NOT NULL,
    error_message TEXT,
    attempted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_probe_runs_domain_id ON probe_runs(domain_id);
CREATE INDEX IF NOT EXISTS idx_probe_runs_created_at ON probe_runs(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_incidents_domain_id ON incidents(domain_id);
CREATE INDEX IF NOT EXISTS idx_incidents_status ON incidents(status);
CREATE INDEX IF NOT EXISTS idx_notifications_status ON notifications(status);
CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON notifications(created_at);

-- Group-related indexes
CREATE INDEX IF NOT EXISTS "IX_domain_groups_enabled" ON domain_groups(enabled);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_domain_groups_name" ON domain_groups(name);
CREATE INDEX IF NOT EXISTS "IX_domains_group_id" ON domains(group_id);
