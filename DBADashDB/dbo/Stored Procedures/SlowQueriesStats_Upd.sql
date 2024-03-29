﻿CREATE PROC [dbo].[SlowQueriesStats_Upd](@SlowQueriesStats dbo.SlowQueriesStats READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
DECLARE @Ref VARCHAR(30)='SlowQueriesStats'

INSERT INTO dbo.SlowQueriesStats
(
	InstanceID,
	SnapshotDate,
	Truncated,
	ProcessingTime,
	TotalEventsProcessed,
	EventCount,
	DroppedCount,
	MemoryUsed 
)
SELECT @InstanceID,
	@SnapshotDate,
		Truncated,
	ProcessingTime,
	TotalEventsProcessed,
	EventCount,
	DroppedCount,
	MemoryUsed 
FROM @SlowQueriesStats t
WHERE NOT EXISTS(SELECT 1 FROM dbo.SlowQueriesStats SQS WHERE SQS.InstanceID = @InstanceID AND SQS.SnapshotDate = @SnapshotDate)

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate