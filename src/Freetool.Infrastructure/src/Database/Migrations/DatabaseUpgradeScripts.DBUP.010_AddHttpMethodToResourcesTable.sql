-- Add HttpMethod column to Resources table
ALTER TABLE Resources ADD COLUMN HttpMethod TEXT NOT NULL DEFAULT 'GET';