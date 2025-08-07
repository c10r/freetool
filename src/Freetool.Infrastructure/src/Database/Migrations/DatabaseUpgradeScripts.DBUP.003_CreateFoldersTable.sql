-- Create Folders table with self-referencing parent-child relationship
CREATE TABLE Folders (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    ParentId TEXT NULL, -- Self-reference for parent-child hierarchy
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (ParentId) REFERENCES Folders(Id) ON DELETE CASCADE
);

-- Create unique index on (Name, ParentId) for sibling uniqueness
-- This ensures that folders with the same parent cannot have duplicate names
CREATE UNIQUE INDEX IX_Folders_Name_ParentId ON Folders (Name, ParentId);

-- Add indexes for common queries
CREATE INDEX IX_Folders_ParentId ON Folders (ParentId);
CREATE INDEX IX_Folders_CreatedAt ON Folders (CreatedAt);
CREATE INDEX IX_Folders_UpdatedAt ON Folders (UpdatedAt);