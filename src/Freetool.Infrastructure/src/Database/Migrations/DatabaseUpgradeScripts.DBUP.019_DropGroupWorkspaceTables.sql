-- Migration: Drop legacy Groups, UserGroups, and Workspaces tables
-- These tables were replaced by Spaces and SpaceMembers in migration 015
-- Data was already migrated, these tables are now unused

-- Drop the junction table first (due to FK constraints)
DROP TABLE IF EXISTS UserGroups;

-- Drop the main tables
DROP TABLE IF EXISTS Workspaces;
DROP TABLE IF EXISTS Groups;
