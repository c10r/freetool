-- Update Resource unique index to:
-- 1. Include SpaceId (allowing same name across different spaces)
-- 2. Exclude soft-deleted records (allowing re-creation after deletion)

-- Drop the existing unique index
DROP INDEX IF EXISTS IX_Resources_Name;

-- Create the new unique index with SpaceId and filter for non-deleted resources
CREATE UNIQUE INDEX IX_Resources_Name_SpaceId ON Resources (Name, SpaceId) WHERE IsDeleted = 0;
