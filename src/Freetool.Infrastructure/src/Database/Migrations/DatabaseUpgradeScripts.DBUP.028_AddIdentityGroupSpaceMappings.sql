-- Create group->space mapping table used for IAP JIT provisioning
CREATE TABLE IF NOT EXISTS IdentityGroupSpaceMappings (
    Id TEXT NOT NULL PRIMARY KEY,
    GroupKey TEXT NOT NULL,
    SpaceId TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedByUserId TEXT NOT NULL,
    UpdatedByUserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (SpaceId) REFERENCES Spaces(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_IdentityGroupSpaceMappings_GroupKey_SpaceId
ON IdentityGroupSpaceMappings(GroupKey, SpaceId);

CREATE INDEX IF NOT EXISTS IX_IdentityGroupSpaceMappings_GroupKey
ON IdentityGroupSpaceMappings(GroupKey);
