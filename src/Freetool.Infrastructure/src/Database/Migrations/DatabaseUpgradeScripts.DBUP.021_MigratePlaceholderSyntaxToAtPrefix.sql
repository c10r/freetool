-- Migrate placeholder syntax from {variableName} to @variableName
-- This migration updates the Apps table columns that can contain placeholders:
-- UrlPath, UrlParameters, Headers, Body

-- Note: SQLite doesn't support regex replacement, so we use simple string replacement.
-- This handles the known current_user patterns. Custom variable names will need
-- manual updating if they exist in the database.

-- Update UrlPath column
UPDATE Apps SET UrlPath = replace(UrlPath, '{current_user.email}', '@current_user.email') WHERE UrlPath LIKE '%{current_user.email}%';
UPDATE Apps SET UrlPath = replace(UrlPath, '{current_user.id}', '@current_user.id') WHERE UrlPath LIKE '%{current_user.id}%';
UPDATE Apps SET UrlPath = replace(UrlPath, '{current_user.firstName}', '@current_user.firstName') WHERE UrlPath LIKE '%{current_user.firstName}%';
UPDATE Apps SET UrlPath = replace(UrlPath, '{current_user.lastName}', '@current_user.lastName') WHERE UrlPath LIKE '%{current_user.lastName}%';

-- Update UrlParameters column (JSON array)
UPDATE Apps SET UrlParameters = replace(UrlParameters, '{current_user.email}', '@current_user.email') WHERE UrlParameters LIKE '%{current_user.email}%';
UPDATE Apps SET UrlParameters = replace(UrlParameters, '{current_user.id}', '@current_user.id') WHERE UrlParameters LIKE '%{current_user.id}%';
UPDATE Apps SET UrlParameters = replace(UrlParameters, '{current_user.firstName}', '@current_user.firstName') WHERE UrlParameters LIKE '%{current_user.firstName}%';
UPDATE Apps SET UrlParameters = replace(UrlParameters, '{current_user.lastName}', '@current_user.lastName') WHERE UrlParameters LIKE '%{current_user.lastName}%';

-- Update Headers column (JSON array)
UPDATE Apps SET Headers = replace(Headers, '{current_user.email}', '@current_user.email') WHERE Headers LIKE '%{current_user.email}%';
UPDATE Apps SET Headers = replace(Headers, '{current_user.id}', '@current_user.id') WHERE Headers LIKE '%{current_user.id}%';
UPDATE Apps SET Headers = replace(Headers, '{current_user.firstName}', '@current_user.firstName') WHERE Headers LIKE '%{current_user.firstName}%';
UPDATE Apps SET Headers = replace(Headers, '{current_user.lastName}', '@current_user.lastName') WHERE Headers LIKE '%{current_user.lastName}%';

-- Update Body column (JSON array)
UPDATE Apps SET Body = replace(Body, '{current_user.email}', '@current_user.email') WHERE Body LIKE '%{current_user.email}%';
UPDATE Apps SET Body = replace(Body, '{current_user.id}', '@current_user.id') WHERE Body LIKE '%{current_user.id}%';
UPDATE Apps SET Body = replace(Body, '{current_user.firstName}', '@current_user.firstName') WHERE Body LIKE '%{current_user.firstName}%';
UPDATE Apps SET Body = replace(Body, '{current_user.lastName}', '@current_user.lastName') WHERE Body LIKE '%{current_user.lastName}%';

-- IMPORTANT: For custom app input variables (e.g., {MyVariable}), you need to manually update them.
-- Example: UPDATE Apps SET Headers = replace(Headers, '{MyVariable}', '@MyVariable') WHERE Headers LIKE '%{MyVariable}%';
-- Or clear your development database and recreate apps with the new syntax.
