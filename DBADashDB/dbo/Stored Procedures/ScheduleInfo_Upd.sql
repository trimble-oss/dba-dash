CREATE PROC [dbo].[ScheduleInfo_Upd](
	@ConnectionID NVARCHAR(128),
	@CollectAgentID INT,
	@ImportAgentID INT,
	@SnapshotDate DATETIME2(2),
	@ScheduleInfo dbo.ScheduleInfo READONLY,
	@HasPauseInfo BIT=0, /* Older services don't report a pause - leave CollectionsPausedUntil alone */
	@CollectionsPausedUntil DATETIME2(3)=NULL
)
AS
SET XACT_ABORT ON

-- 1. Ensure the Instance exists (Stub creation)
DECLARE @InstanceID INT = (SELECT InstanceID FROM dbo.Instances WHERE ConnectionID = @ConnectionID)

IF @InstanceID IS NULL
BEGIN
	BEGIN TRY
		INSERT INTO dbo.Instances(ConnectionID,Instance,IsActive,CollectAgentID,ImportAgentID)
		VALUES(@ConnectionID, @ConnectionID, CAST(1 AS BIT), @CollectAgentID, @ImportAgentID)
		SET @InstanceID = SCOPE_IDENTITY()
	END TRY
	BEGIN CATCH
		-- Handle concurrent insert race condition gracefully
		IF ERROR_NUMBER() NOT IN (2601,2627) THROW;
		SET @InstanceID = (SELECT InstanceID FROM dbo.Instances WHERE ConnectionID = @ConnectionID)
	END CATCH
END

-- 2. Process Schedule Info Update
DECLARE @Ref VARCHAR(30)='ScheduleInfo'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN

	DELETE FROM dbo.ScheduleInfo
	WHERE InstanceID = @InstanceID

	INSERT INTO dbo.ScheduleInfo
	(
		InstanceID,
		Reference,
		Schedule,
		RunOnServiceStart,
		MaxIntervalMinutes,
		IsInstanceOverride,
		SnapshotDate,
		TemporaryUntil
	)
	SELECT @InstanceID,
		S.Reference,
		S.Schedule,
		S.RunOnServiceStart,
		S.MaxIntervalMinutes,
		S.IsInstanceOverride,
		@SnapshotDate,
		S.TemporaryUntil
	FROM @ScheduleInfo S

	IF @HasPauseInfo = 1
	BEGIN
		/*	CollectionsPausedUntil holds the end of the latest pause rather than going back to NULL.  When the service
			reports no pause, a pause still in effect here has been resumed early, so it ends now.  Keeping the end time
			gives collections a grace period after a pause - see CollectionDatesStatus */
		UPDATE dbo.Instances
		SET CollectionsPausedUntil = Calc.CollectionsPausedUntil
		FROM dbo.Instances
		CROSS APPLY(SELECT CASE WHEN @CollectionsPausedUntil IS NOT NULL THEN @CollectionsPausedUntil
							WHEN Instances.CollectionsPausedUntil > SYSUTCDATETIME() THEN SYSUTCDATETIME()
							ELSE Instances.CollectionsPausedUntil END AS CollectionsPausedUntil) Calc
		WHERE InstanceID = @InstanceID
		AND EXISTS(SELECT Instances.CollectionsPausedUntil EXCEPT SELECT Calc.CollectionsPausedUntil)
	END

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
	COMMIT
END
