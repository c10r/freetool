-- Create Workspaces table
CREATE TABLE Workspaces (
    Id TEXT NOT NULL PRIMARY KEY,
    GroupId TEXT NOT NULL UNIQUE,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (GroupId) REFERENCES Groups(Id)
);

-- Create index on GroupId
CREATE UNIQUE INDEX IX_Workspaces_GroupId ON Workspaces(GroupId);

-- Create index on IsDeleted for soft delete filtering
CREATE INDEX IX_Workspaces_IsDeleted ON Workspaces(IsDeleted);

-- Add WorkspaceId column to Folders table
-- Note: We need to handle existing folders by creating a default workspace for migration
-- First, create a temporary column
ALTER TABLE Folders ADD COLUMN WorkspaceId_New TEXT;

-- For the migration, we'll add the constraint after data is migrated
-- This needs to be done by the application or a manual data migration step
-- For now, we'll just add the column

-- Rename the new column to replace the old one (SQLite doesn't support ALTER COLUMN)
-- We'll need to recreate the table with the new schema

-- Save existing folder data
CREATE TABLE Folders_backup (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    ParentId TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);

INSERT INTO Folders_backup SELECT Id, Name, ParentId, CreatedAt, UpdatedAt, IsDeleted FROM Folders;

-- Drop old table
DROP TABLE Folders;

-- Recreate Folders table with WorkspaceId
CREATE TABLE Folders (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    ParentId TEXT,
    WorkspaceId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id),
    FOREIGN KEY (ParentId) REFERENCES Folders(Id)
);

-- Note: The data migration needs to be handled separately
-- After creating workspaces for existing groups, run:
-- INSERT INTO Folders SELECT Id, Name, ParentId, '<workspace-id>', CreatedAt, UpdatedAt, IsDeleted FROM Folders_backup;

-- Recreate indexes
CREATE UNIQUE INDEX IX_Folders_Name_ParentId ON Folders(Name, ParentId);
CREATE INDEX IX_Folders_IsDeleted ON Folders(IsDeleted);
CREATE INDEX IX_Folders_WorkspaceId ON Folders(WorkspaceId);

-- Drop backup table (comment out if you want to keep it for manual data migration)
-- DROP TABLE Folders_backup;
