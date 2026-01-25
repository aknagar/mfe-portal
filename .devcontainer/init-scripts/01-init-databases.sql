-- Initialize databases for MFE Portal development
-- This script runs automatically when the PostgreSQL container starts

-- Create productdb if it doesn't exist
SELECT 'CREATE DATABASE productdb'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'productdb')\gexec

-- Create weatherdb if it doesn't exist  
SELECT 'CREATE DATABASE weatherdb'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'weatherdb')\gexec

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE productdb TO postgres;
GRANT ALL PRIVILEGES ON DATABASE weatherdb TO postgres;
