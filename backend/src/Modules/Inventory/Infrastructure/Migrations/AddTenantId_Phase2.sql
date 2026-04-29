-- US-081: Multi-tenancy Phase 2 — Add TenantId to Inventory tables
-- Run this script against your PostgreSQL database after deploying the updated application.

-- Products
ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "TenantId" VARCHAR(50) NULL;
CREATE INDEX IF NOT EXISTS "IX_Products_TenantId" ON "Products" ("TenantId");

-- Suppliers
ALTER TABLE "Suppliers" ADD COLUMN IF NOT EXISTS "TenantId" VARCHAR(50) NULL;
CREATE INDEX IF NOT EXISTS "IX_Suppliers_TenantId" ON "Suppliers" ("TenantId");

-- Locations
ALTER TABLE "Locations" ADD COLUMN IF NOT EXISTS "TenantId" VARCHAR(50) NULL;
CREATE INDEX IF NOT EXISTS "IX_Locations_TenantId" ON "Locations" ("TenantId");

-- StockMovements
ALTER TABLE "StockMovements" ADD COLUMN IF NOT EXISTS "TenantId" VARCHAR(50) NULL;
CREATE INDEX IF NOT EXISTS "IX_StockMovements_TenantId" ON "StockMovements" ("TenantId");

-- Optional: backfill existing rows with a default tenant
-- UPDATE "Products" SET "TenantId" = 'tenant-alpha' WHERE "TenantId" IS NULL;
-- UPDATE "Suppliers" SET "TenantId" = 'tenant-alpha' WHERE "TenantId" IS NULL;
-- UPDATE "Locations" SET "TenantId" = 'tenant-alpha' WHERE "TenantId" IS NULL;
-- UPDATE "StockMovements" SET "TenantId" = 'tenant-alpha' WHERE "TenantId" IS NULL;
