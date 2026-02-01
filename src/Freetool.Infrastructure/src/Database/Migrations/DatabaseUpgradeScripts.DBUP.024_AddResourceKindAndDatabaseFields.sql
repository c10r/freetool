-- Add ResourceKind and database configuration fields to Resources
-- Also allow BaseUrl to be nullable for SQL resources

CREATE TABLE Resources_new (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    ResourceKind TEXT NOT NULL DEFAULT 'HTTP',
    BaseUrl TEXT,
    UrlParameters TEXT NOT NULL DEFAULT '[]',
    Headers TEXT NOT NULL DEFAULT '[]',
    Body TEXT NOT NULL DEFAULT '[]',
    DatabaseName TEXT,
    DatabaseHost TEXT,
    DatabasePort INTEGER,
    DatabaseAuthScheme TEXT,
    DatabaseUsername TEXT,
    DatabasePassword TEXT,
    UseSsl INTEGER NOT NULL DEFAULT 0,
    EnableSshTunnel INTEGER NOT NULL DEFAULT 0,
    ConnectionOptions TEXT NOT NULL DEFAULT '[]',
    SpaceId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SpaceId) REFERENCES Spaces(Id)
);

INSERT INTO Resources_new (
    Id,
    Name,
    Description,
    ResourceKind,
    BaseUrl,
    UrlParameters,
    Headers,
    Body,
    DatabaseName,
    DatabaseHost,
    DatabasePort,
    DatabaseAuthScheme,
    DatabaseUsername,
    DatabasePassword,
    UseSsl,
    EnableSshTunnel,
    ConnectionOptions,
    SpaceId,
    CreatedAt,
    UpdatedAt,
    IsDeleted
)
SELECT
    Id,
    Name,
    Description,
    'HTTP',
    BaseUrl,
    UrlParameters,
    Headers,
    Body,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    0,
    0,
    '[]',
    SpaceId,
    CreatedAt,
    UpdatedAt,
    IsDeleted
FROM Resources;

DROP TABLE Resources;
ALTER TABLE Resources_new RENAME TO Resources;

CREATE UNIQUE INDEX IX_Resources_Name_SpaceId ON Resources (Name, SpaceId) WHERE IsDeleted = 0;
CREATE INDEX IX_Resources_CreatedAt ON Resources (CreatedAt);
CREATE INDEX IX_Resources_UpdatedAt ON Resources (UpdatedAt);
CREATE INDEX IX_Resources_SpaceId ON Resources (SpaceId);
