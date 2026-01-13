-- Add UseDynamicJsonBody column to Apps table
-- When true, the app's body parameters are provided at runtime as JSON
ALTER TABLE Apps ADD COLUMN UseDynamicJsonBody INTEGER NOT NULL DEFAULT 0;
