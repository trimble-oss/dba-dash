CREATE PROC CPU_Upd(@CPU dbo.CPU READONLY,@InstanceID INT,@SnapshotDate DATETIME)
AS
INSERT INTO dbo.CPU(InstanceID,EventTime,SQLProcessCPU,SystemIdleCPU)
SELECT @InstanceID,EventTime,SQLProcessCPU,SystemIdleCPU 
FROM @CPU t
WHERE NOT EXISTS(SELECT 1 FROM dbo.CPU WHERE InstanceID = @InstanceID AND CPU.EventTime = t.EventTime)