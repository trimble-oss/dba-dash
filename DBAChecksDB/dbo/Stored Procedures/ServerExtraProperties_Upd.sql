CREATE PROC [dbo].[ServerExtraProperties_Upd]
(
    @InstanceID INT,
    @SnapshotDate DATETIME,
    @ActivePowerPlanGUID UNIQUEIDENTIFIER,
    @ActivePowerPlan VARCHAR(16),
    @ProcessorNameString NVARCHAR(512),
    @SystemManufacturer NVARCHAR(512),
    @SystemProductName NVARCHAR(512),
    @IsAgentRunning BIT,
    @InstantFileInitializationEnabled BIT,
	@OfflineSchedulers INT=NULL
)
AS
DECLARE @Ref VARCHAR(30)='ServerExtraProperties'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	UPDATE dbo.Instances
	SET ActivePowerPlanGUID = @ActivePowerPlanGUID,
		ActivePowerPlan = @ActivePowerPlan,
		ProcessorNameString = @ProcessorNameString,
		SystemManufacturer = @SystemManufacturer,
		SystemProductName = @SystemProductName,
		IsAgentRunning = @IsAgentRunning,
		InstantFileInitializationEnabled = @InstantFileInitializationEnabled,
		OfflineSchedulers=@OfflineSchedulers
	WHERE InstanceID = @InstanceID;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END