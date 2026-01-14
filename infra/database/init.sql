-- Docker initialization script
-- This script runs schema.sql on first container startup

\i /docker-entrypoint-initdb.d/schema.sql
