CREATE PROC dbo.Counters_Get
AS
SELECT CounterID,
       object_name,
       counter_name,
       instance_name 
FROM dbo.Counters C
WHERE EXISTS(SELECT 1 
			FROM dbo.InstanceCounters IC 
			WHERE IC.CounterID = C.CounterID
			AND IC.UpdatedDate>=DATEADD(d,-2,GETUTCDATE())
			)