﻿CREATE PROC Jobs_Get(@InstanceID INT)
AS
SELECT job_id,name,enabled
FROM dbo.Jobs 
WHERE InstanceID = @InstanceID 
AND IsActive=1
ORDER BY name