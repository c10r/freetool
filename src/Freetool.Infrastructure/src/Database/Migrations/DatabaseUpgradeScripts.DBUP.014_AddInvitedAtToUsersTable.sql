-- Add InvitedAt column to Users table for tracking invited (placeholder) users
ALTER TABLE Users ADD COLUMN InvitedAt TEXT NULL;

-- Create index for efficient lookup of invited users
CREATE INDEX IX_Users_InvitedAt ON Users (InvitedAt);
