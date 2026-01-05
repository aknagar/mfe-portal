-- Migration: 001_initial_schema
-- Description: Create initial schema for Forecast and Product tables
-- Created: 2026-01-04

-- Use advisory lock to ensure idempotent execution
SELECT pg_advisory_lock(1);

BEGIN;

-- Create Forecast table if it doesn't exist
CREATE TABLE IF NOT EXISTS public."Forecasts" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Date" date NOT NULL,
    "TemperatureC" integer NOT NULL,
    "Summary" character varying(500),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_Forecasts" PRIMARY KEY ("Id")
);

-- Create Product table if it doesn't exist
CREATE TABLE IF NOT EXISTS public."Product" (
    "Id" integer NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    "Name" character varying(255),
    "Description" text,
    "Price" numeric(18, 2) NOT NULL DEFAULT 0.0,
    "ImageUrl" character varying(500),
    CONSTRAINT "PK_Product" PRIMARY KEY ("Id")
);

COMMIT;

-- Release advisory lock
SELECT pg_advisory_unlock(1);
