-- Add missing columns to Apps table
ALTER TABLE Apps ADD COLUMN ResourceId TEXT NOT NULL DEFAULT '';
ALTER TABLE Apps ADD COLUMN UrlPath TEXT;
ALTER TABLE Apps ADD COLUMN UrlParameters TEXT NOT NULL DEFAULT '[]'; -- JSON serialized key-value pairs
ALTER TABLE Apps ADD COLUMN Headers TEXT NOT NULL DEFAULT '[]'; -- JSON serialized key-value pairs  
ALTER TABLE Apps ADD COLUMN Body TEXT NOT NULL DEFAULT '[]'; -- JSON serialized key-value pairs

-- Create foreign key index on ResourceId
CREATE INDEX IX_Apps_ResourceId ON Apps (ResourceId);

-- Create index on IsDeleted for efficient queries
CREATE INDEX IX_Apps_IsDeleted ON Apps (IsDeleted);