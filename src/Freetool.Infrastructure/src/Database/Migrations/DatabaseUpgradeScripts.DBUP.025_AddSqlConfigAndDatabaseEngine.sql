-- Add DatabaseEngine to Resources
ALTER TABLE Resources ADD COLUMN DatabaseEngine TEXT;

-- Default existing SQL resources to Postgres
UPDATE Resources SET DatabaseEngine = 'POSTGRES' WHERE ResourceKind = 'SQL';

-- Add SqlConfig to Apps
ALTER TABLE Apps ADD COLUMN SqlConfig TEXT;
