-- Create Apps table
CREATE TABLE Apps (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    FolderId TEXT NOT NULL,
    Inputs TEXT NOT NULL, -- JSON serialized list of inputs
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (FolderId) REFERENCES Folders(Id)
);

-- Create unique index on Name within FolderId (name unique to folder)
CREATE UNIQUE INDEX IX_Apps_Name_FolderId ON Apps (Name, FolderId);

-- Create index on FolderId for efficient folder queries
CREATE INDEX IX_Apps_FolderId ON Apps (FolderId);

-- Add indexes for common queries
CREATE INDEX IX_Apps_CreatedAt ON Apps (CreatedAt);
CREATE INDEX IX_Apps_UpdatedAt ON Apps (UpdatedAt);