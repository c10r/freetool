CREATE TABLE IF NOT EXISTS Dashboards (
    Id TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL,
    FolderId TEXT NOT NULL,
    PrepareAppId TEXT NULL,
    Configuration TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (FolderId) REFERENCES Folders(Id) ON DELETE RESTRICT,
    FOREIGN KEY (PrepareAppId) REFERENCES Apps(Id) ON DELETE SET NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Dashboards_Name_FolderId_IsDeleted
ON Dashboards(Name, FolderId, IsDeleted);

CREATE INDEX IF NOT EXISTS IX_Dashboards_FolderId
ON Dashboards(FolderId);

CREATE INDEX IF NOT EXISTS IX_Dashboards_PrepareAppId
ON Dashboards(PrepareAppId);
