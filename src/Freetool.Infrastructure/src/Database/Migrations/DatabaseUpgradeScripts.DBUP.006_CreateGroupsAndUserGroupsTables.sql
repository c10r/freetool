-- Create Groups table
CREATE TABLE Groups (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);

-- Create unique index on Group Name (for unique group names)
CREATE UNIQUE INDEX IX_Groups_Name ON Groups (Name) WHERE IsDeleted = 0;

-- Add indexes for common queries
CREATE INDEX IX_Groups_CreatedAt ON Groups (CreatedAt);
CREATE INDEX IX_Groups_UpdatedAt ON Groups (UpdatedAt);
CREATE INDEX IX_Groups_IsDeleted ON Groups (IsDeleted);

-- Create UserGroups junction table for many-to-many relationship
CREATE TABLE UserGroups (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    GroupId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (GroupId) REFERENCES Groups(Id) ON DELETE CASCADE
);

-- Create unique index to prevent duplicate user-group relationships
CREATE UNIQUE INDEX IX_UserGroups_UserId_GroupId ON UserGroups (UserId, GroupId);

-- Add indexes for efficient queries
CREATE INDEX IX_UserGroups_UserId ON UserGroups (UserId);
CREATE INDEX IX_UserGroups_GroupId ON UserGroups (GroupId);
CREATE INDEX IX_UserGroups_CreatedAt ON UserGroups (CreatedAt);