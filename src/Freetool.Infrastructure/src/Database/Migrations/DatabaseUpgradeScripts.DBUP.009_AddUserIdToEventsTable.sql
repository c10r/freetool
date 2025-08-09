-- Add UserId column to Events table for audit logging
ALTER TABLE Events ADD COLUMN UserId TEXT NOT NULL;

-- Create index for efficient querying by UserId
CREATE INDEX IX_Events_UserId ON Events(UserId);

-- Create composite index for efficient queries combining EntityType and UserId
CREATE INDEX IX_Events_EntityType_UserId ON Events(EntityType, UserId);