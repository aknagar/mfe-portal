-- Migration: 002_add_indexes
-- Description: Add indexes for performance optimization
-- Created: 2026-01-04

-- Use advisory lock to ensure idempotent execution
SELECT pg_advisory_lock(2);

BEGIN;

-- Create indexes for Forecast table
CREATE INDEX IF NOT EXISTS "IX_Forecasts_Date" ON public."Forecasts" ("Date");
CREATE INDEX IF NOT EXISTS "IX_Forecasts_IsDeleted" ON public."Forecasts" ("IsDeleted");

-- Create indexes for Product table
CREATE INDEX IF NOT EXISTS "IX_Product_Name" ON public."Product" ("Name");

COMMIT;

-- Release advisory lock
SELECT pg_advisory_unlock(2);
