-- Migration: Unify Group and Workspace into Space
-- This migration creates the new Spaces and SpaceMembers tables,
-- migrates data from Groups/UserGroups, and adds SpaceId to related tables.

-- Create Spaces table
CREATE TABLE Spaces (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    ModeratorUserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);

-- Create SpaceMembers junction table
CREATE TABLE SpaceMembers (
    Id TEXT NOT NULL PRIMARY KEY,
    UserId TEXT NOT NULL,
    SpaceId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (SpaceId) REFERENCES Spaces(Id) ON DELETE CASCADE
);

-- Create indexes
CREATE UNIQUE INDEX IX_Spaces_Name ON Spaces(Name) WHERE IsDeleted = 0;
CREATE UNIQUE INDEX IX_SpaceMembers_UserId_SpaceId ON SpaceMembers(UserId, SpaceId);
CREATE INDEX IX_SpaceMembers_SpaceId ON SpaceMembers(SpaceId);

-- Migrate data from Groups -> Spaces
-- Each Group becomes a Space with same Id for FK continuity
-- First user in group becomes Moderator (or fallback to first active user if group is empty)
INSERT INTO Spaces (Id, Name, ModeratorUserId, CreatedAt, UpdatedAt, IsDeleted)
SELECT
    g.Id,
    g.Name,
    COALESCE(
        (SELECT ug.UserId FROM UserGroups ug WHERE ug.GroupId = g.Id LIMIT 1),
        (SELECT u.Id FROM Users u WHERE u.IsDeleted = 0 LIMIT 1)
    ) as ModeratorUserId,
    g.CreatedAt,
    g.UpdatedAt,
    g.IsDeleted
FROM Groups g;

-- Migrate UserGroups -> SpaceMembers
INSERT INTO SpaceMembers (Id, UserId, SpaceId, CreatedAt)
SELECT Id, UserId, GroupId, CreatedAt FROM UserGroups;

-- Add SpaceId column to Folders (mapping from WorkspaceId via Workspaces.GroupId)
ALTER TABLE Folders ADD COLUMN SpaceId TEXT;
UPDATE Folders SET SpaceId = (
    SELECT w.GroupId FROM Workspaces w WHERE w.Id = Folders.WorkspaceId
);

-- Add SpaceId column to Resources
ALTER TABLE Resources ADD COLUMN SpaceId TEXT;
UPDATE Resources SET SpaceId = (
    SELECT w.GroupId FROM Workspaces w WHERE w.Id = Resources.WorkspaceId
);

-- Note: Old tables (Groups, UserGroups, Workspaces) kept for rollback capability
-- Can be dropped in a future migration after verification
