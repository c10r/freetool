-- Create Resources table
CREATE TABLE Resources (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    BaseUrl TEXT NOT NULL,
    UrlParameters TEXT NOT NULL DEFAULT '[]',  -- JSON array of key-value pairs
    Headers TEXT NOT NULL DEFAULT '[]',        -- JSON array of key-value pairs
    Body TEXT NOT NULL DEFAULT '[]',           -- JSON array of key-value pairs
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);

-- Create unique index on Name
CREATE UNIQUE INDEX IX_Resources_Name ON Resources (Name);

-- Add indexes for common queries
CREATE INDEX IX_Resources_CreatedAt ON Resources (CreatedAt);
CREATE INDEX IX_Resources_UpdatedAt ON Resources (UpdatedAt);