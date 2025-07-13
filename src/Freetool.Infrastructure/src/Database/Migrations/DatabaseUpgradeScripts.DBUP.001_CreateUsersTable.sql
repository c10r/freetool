-- Create Users table
CREATE TABLE Users (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    Email TEXT NOT NULL,
    ProfilePicUrl TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

-- Create unique index on Email
CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);

-- Add indexes for common queries
CREATE INDEX IX_Users_CreatedAt ON Users (CreatedAt);
CREATE INDEX IX_Users_UpdatedAt ON Users (UpdatedAt);