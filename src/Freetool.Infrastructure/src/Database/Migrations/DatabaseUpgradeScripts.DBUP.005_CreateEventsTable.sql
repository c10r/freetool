-- Create Events table for audit logging
CREATE TABLE Events (
    Id TEXT NOT NULL PRIMARY KEY,
    EventId TEXT NOT NULL UNIQUE,
    EventType TEXT NOT NULL,
    EntityType TEXT NOT NULL,
    EntityId TEXT NOT NULL,
    EventData TEXT NOT NULL, -- JSON serialized event data
    OccurredAt TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for efficient querying
CREATE INDEX IX_Events_EntityType_EntityId ON Events(EntityType, EntityId);
CREATE INDEX IX_Events_OccurredAt ON Events(OccurredAt);
CREATE INDEX IX_Events_EventType ON Events(EventType);
CREATE INDEX IX_Events_CreatedAt ON Events(CreatedAt);