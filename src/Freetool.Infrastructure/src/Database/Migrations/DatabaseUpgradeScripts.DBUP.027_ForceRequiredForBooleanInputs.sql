-- Ensure boolean inputs are always required
-- Updates Apps.Inputs JSON where Boolean inputs were marked as optional

UPDATE Apps
SET Inputs = replace(Inputs, '"Type":"Boolean","Required":false', '"Type":"Boolean","Required":true')
WHERE Inputs LIKE '%"Type":"Boolean","Required":false%';
