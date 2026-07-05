CREATE PROC [dbo].[ScheduleInfo_Upd](
	@ConnectionID NVARCHAR(128),
	@CollectAgentID INT,
	@ImportAgentID INT,
	@SnapshotDate DATETIME2(2),
	@ScheduleInfo dbo.ScheduleInfo READONLY
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
		SnapshotDate
	)
	SELECT @InstanceID,
		S.Reference,
		S.Schedule,
		S.RunOnServiceStart,
		S.MaxIntervalMinutes,
		S.IsInstanceOverride,
		@SnapshotDate
	FROM @ScheduleInfo S

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
	COMMIT
END
