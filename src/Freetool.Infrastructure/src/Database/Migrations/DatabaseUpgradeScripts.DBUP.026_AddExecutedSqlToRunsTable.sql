-- Add ExecutedSql column to Runs table (stores interpolated SQL for SQL runs)
ALTER TABLE Runs ADD COLUMN ExecutedSql TEXT;
