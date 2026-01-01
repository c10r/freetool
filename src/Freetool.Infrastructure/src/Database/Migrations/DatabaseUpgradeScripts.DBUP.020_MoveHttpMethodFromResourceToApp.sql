-- Add HttpMethod column to Apps (required, no default - empty string placeholder)
ALTER TABLE Apps ADD COLUMN HttpMethod TEXT NOT NULL DEFAULT '';

-- Remove HttpMethod column from Resources
-- Note: SQLite doesn't support DROP COLUMN directly, but newer versions (3.35+) do
-- Using the newer syntax as we're on a modern SQLite version
ALTER TABLE Resources DROP COLUMN HttpMethod;
