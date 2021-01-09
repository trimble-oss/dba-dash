
CREATE PROC [Report].[CPU](@InstanceID INT=NULL,@Mins INT=60)
AS
SELECT              CPU.EventTime,
                  CPU.SQLProcessCPU,
                  CPU.SystemIdleCPU 
FROM dbo.CPU
JOIN dbo.Instances I ON I.InstanceID= CPU.InstanceID 
WHERE I.IsActive=1
AND CPU.EventTime >= DATEADD(mi,-@Mins,GETUTCDATE())
AND I.InstanceID=@InstanceID
OPTION(RECOMPILE)