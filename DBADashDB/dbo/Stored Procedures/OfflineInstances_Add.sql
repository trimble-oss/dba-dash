CREATE PROC dbo.OfflineInstances_Add(
	@OfflineInstances dbo.OfflineInstances READONLY,
	@CollectAgentID INT,
	@ImportAgentID INT,
	@SnapshotDate DATETIME2
)
AS
SET XACT_ABORT ON
INSERT INTO dbo.ErrorMessage(Message)
SELECT DISTINCT Message
FROM (
	SELECT LEFT(FirstMessage,500) AS Message
	FROM @OfflineInstances
	UNION
	SELECT LEFT(LastMessage,500) AS Message
	FROM @OfflineInstances
) T
WHERE NOT EXISTS(
				SELECT 1 
				FROM dbo.ErrorMessage EM
				WHERE EM.Message=T.Message
				)

/* Add instance if it doesn't exist so we can track new connections that are not working*/
INSERT INTO dbo.Instances(ConnectionID,Instance,IsActive,CollectAgentID,ImportAgentID)
SELECT	T.ConnectionID,
		T.ConnectionID,
		CAST(1 AS BIT),
		@CollectAgentID,
		@ImportAgentID
FROM @OfflineInstances T
WHERE NOT EXISTS(
	SELECT 1 
	FROM dbo.Instances I 
	WHERE T.ConnectionID = I.ConnectionID
	)

BEGIN TRAN

/*	
	Close offline incidents if instance is collected by this agent and 
	the instance isn't in @OfflineInstances or the instance is in @OfflineInstances but the service has restarted (FirstFail is different)
*/
UPDATE OI
	SET OI.IsCurrent=CAST(0 AS BIT),
	ClosedDate = ISNULL(T.FirstFail,@SnapshotDate)
FROM dbo.OfflineInstances OI
JOIN dbo.Instances I ON OI.InstanceID =I.InstanceID
LEFT JOIN @OfflineInstances T ON T.ConnectionID = I.ConnectionID
WHERE I.CollectAgentID=@CollectAgentID
AND OI.IsCurrent=1
AND (OI.FirstFail < T.FirstFail OR T.FirstFail IS NULL)
	
/* Start tracking new instances that are offline */
INSERT INTO dbo.OfflineInstances(
	InstanceID,
	FirstFail,
	LastFail,
	IsCurrent,
	FirstMessageID,
	LastMessageID,
	FailCount
)
SELECT	I.InstanceID,
		O.FirstFail,
		O.LastFail,
		CAST(1 AS BIT),
		EF.ErrorMessageID,
		EL.ErrorMessageID,
		O.FailCount
FROM @OfflineInstances O
JOIN dbo.Instances I ON O.ConnectionID = I.ConnectionID
LEFT JOIN dbo.ErrorMessage EF ON LEFT(O.FirstMessage,500) = EF.Message
LEFT JOIN dbo.ErrorMessage EL ON LEFT(O.LastMessage,500) = EL.Message
WHERE NOT EXISTS(
				SELECT 1 
				FROM dbo.OfflineInstances OI
				WHERE OI.InstanceID=I.InstanceID 
				AND OI.IsCurrent=1
				)

/* Update existing offline instances*/
UPDATE OI 
	SET OI.IsCurrent=1,
	OI.LastFail = T.LastFail,
	ClosedDate = NULL,
	LastMessageID = EL.ErrorMessageID,
	FailCount = T.FailCount
FROM @OfflineInstances T 
JOIN dbo.Instances I ON T.ConnectionID = I.ConnectionID
JOIN dbo.OfflineInstances OI ON I.InstanceID = OI.InstanceID
LEFT JOIN dbo.ErrorMessage EL ON LEFT(T.LastMessage,500) = EL.Message
WHERE OI.FirstFail = T.FirstFail
AND OI.LastFail < T.LastFail

COMMIT