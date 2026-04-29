-- US-081: Multi-tenancy Phase 2 — Add TenantId to Issues tables
-- Run this script against your PostgreSQL database after deploying the updated application.

-- Issues
ALTER TABLE "Issues" ADD COLUMN IF NOT EXISTS "TenantId" VARCHAR(50) NULL;
CREATE INDEX IF NOT EXISTS "IX_Issues_TenantId" ON "Issues" ("TenantId");

-- Optional: backfill existing rows with a default tenant
-- UPDATE "Issues" SET "TenantId" = 'tenant-alpha' WHERE "TenantId" IS NULL;
