-- Drop the existing unique index
DROP INDEX IX_Apps_Name_FolderId;

-- Recreate the unique index with a filter to exclude soft-deleted apps
CREATE UNIQUE INDEX IX_Apps_Name_FolderId ON Apps (Name, FolderId) WHERE IsDeleted = 0;