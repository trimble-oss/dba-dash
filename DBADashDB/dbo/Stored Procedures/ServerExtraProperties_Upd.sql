CREATE PROC [dbo].[ServerExtraProperties_Upd]
(
    @InstanceID INT,
    @SnapshotDate DATETIME2(2),
    @ActivePowerPlanGUID UNIQUEIDENTIFIER,
    @ActivePowerPlan VARCHAR(16),
    @ProcessorNameString NVARCHAR(512),
    @SystemManufacturer NVARCHAR(512),
    @SystemProductName NVARCHAR(512),
    @IsAgentRunning BIT,
    @InstantFileInitializationEnabled BIT,
	@OfflineSchedulers INT=NULL,
	@ResourceGovernorEnabled BIT=NULL,
	@WindowsRelease NVARCHAR(256)= NULL,
	@WindowsSP NVARCHAR(256)= NULL,
	@WindowsSKU NVARCHAR(256)= NULL,
	@LastMemoryDump DATETIME=NULL,
	@MemoryDumpCount INT= NULL,
	@WindowsCaption NVARCHAR(256)= NULL
)
AS
DECLARE @Ref VARCHAR(30)='ServerExtraProperties'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	IF EXISTS(SELECT 1 
			FROM dbo.Instances I
			WHERE I.InstanceID = @InstanceID
			AND (I.SystemProductName <> @SystemProductName
				OR I.SystemManufacturer <> @SystemManufacturer
				OR I.ProcessorNameString <> @ProcessorNameString
				)
			)
	BEGIN
		IF NOT EXISTS(SELECT 1 FROM dbo.HostUpgradeHistory WHERE InstanceID = @InstanceID AND ChangeDate = @SnapshotDate)
		BEGIN
			INSERT INTO dbo.HostUpgradeHistory(InstanceID,ChangeDate)
			VALUES(@InstanceID,@SnapshotDate)
		END
		UPDATE HUH 
			SET HUH.SystemManufacturerOld = I.SystemManufacturer,
			HUH.SystemManufacturerNew = @SystemManufacturer,
			HUH.SystemProductNameOld = I.SystemProductName,
			HUH.SystemProductNameNew = @SystemProductName,
			HUH.Processor_old = I.ProcessorNameString,
			HUH.Processor_new = @ProcessorNameString
		FROM dbo.HostUpgradeHistory HUH 
		JOIN dbo.Instances I ON I.InstanceID = HUH.InstanceID
		WHERE HUH.InstanceID = @InstanceID 
		AND HUH.ChangeDate = @SnapshotDate
		
	END

	UPDATE dbo.Instances
	SET ActivePowerPlanGUID = @ActivePowerPlanGUID,
		ActivePowerPlan = @ActivePowerPlan,
		ProcessorNameString = @ProcessorNameString,
		SystemManufacturer = @SystemManufacturer,
		SystemProductName = @SystemProductName,
		IsAgentRunning = @IsAgentRunning,
		InstantFileInitializationEnabled = @InstantFileInitializationEnabled,
		OfflineSchedulers=@OfflineSchedulers,
		ResourceGovernorEnabled=@ResourceGovernorEnabled,
		WindowsRelease=@WindowsRelease,
		WindowsSP=@WindowsSP,
		WindowsSKU =@WindowsSKU,
		LastMemoryDump =@LastMemoryDump,
		MemoryDumpCount=@MemoryDumpCount,
		WindowsCaption =@WindowsCaption
	WHERE InstanceID = @InstanceID;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END