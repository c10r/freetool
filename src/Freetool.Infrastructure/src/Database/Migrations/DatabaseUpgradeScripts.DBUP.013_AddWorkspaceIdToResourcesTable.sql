-- Add WorkspaceId column to Resources table
-- This makes resources workspace-scoped for proper authorization

-- Step 1: Add the column as nullable first (SQLite doesn't support adding NOT NULL columns directly)
ALTER TABLE Resources ADD COLUMN WorkspaceId TEXT;

-- Step 2: Set a default workspace for any existing resources
-- Note: In production, you'd want to identify the correct workspace for each resource
-- For now, we'll require manual data migration or deletion of existing resources

-- Step 3: We cannot add NOT NULL constraint in SQLite after the fact,
-- so we rely on application-level validation
-- New resources will be created with WorkspaceId set by the application

-- Create index for common workspace-based queries
CREATE INDEX IX_Resources_WorkspaceId ON Resources (WorkspaceId);
