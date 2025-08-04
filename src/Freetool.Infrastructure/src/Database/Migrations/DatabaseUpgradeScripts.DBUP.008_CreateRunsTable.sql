-- Create Runs table
CREATE TABLE Runs (
    Id TEXT NOT NULL PRIMARY KEY,
    AppId TEXT NOT NULL,
    Status TEXT NOT NULL CHECK (Status IN ('Pending', 'Running', 'Success', 'Failure', 'InvalidConfiguration')),
    InputValues TEXT NOT NULL, -- JSON serialized list of RunInputValue
    ExecutableRequest TEXT, -- JSON serialized ExecutableHttpRequest (null until composed)
    Response TEXT, -- HTTP response body (null until completed successfully)
    ErrorMessage TEXT, -- Error message (null unless failed)
    StartedAt TEXT, -- When the run was started (null if not started)
    CompletedAt TEXT, -- When the run was completed (null if not completed)
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (AppId) REFERENCES Apps(Id)
);

-- Create index on AppId for efficient app queries
CREATE INDEX IX_Runs_AppId ON Runs (AppId);

-- Create index on Status for efficient status queries
CREATE INDEX IX_Runs_Status ON Runs (Status);

-- Create indexes for common queries by timestamp
CREATE INDEX IX_Runs_CreatedAt ON Runs (CreatedAt);
CREATE INDEX IX_Runs_StartedAt ON Runs (StartedAt);
CREATE INDEX IX_Runs_CompletedAt ON Runs (CompletedAt);

-- Create composite index for app + status queries
CREATE INDEX IX_Runs_AppId_Status ON Runs (AppId, Status);

-- Create composite index for app + created at for pagination
CREATE INDEX IX_Runs_AppId_CreatedAt ON Runs (AppId, CreatedAt DESC);