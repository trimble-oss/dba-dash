CREATE PROC dbo.Instance_Upd(
	@ConnectionID SYSNAME,
	@Instance SYSNAME,
	@SnapshotDate DATETIME2(2),
	@AgentHostName NVARCHAR(16)=NULL,
	@AgentVersion VARCHAR(30)='',
	@EditionID BIGINT=NULL,
	@HostPlatform NVARCHAR(256)=NULL,
	@HostDistribution NVARCHAR(256)=NULL,
	@HostRelease NVARCHAR(256)=NULL,
	@HostServicePackLevel NVARCHAR(256)=NULL,
	@HostSKU INT=NULL,
	@OSLanguageVersion INT=NULL,
	@UTCOffset INT=NULL,
	@AgentServiceName NVARCHAR(256)='{DBADashAgent}',
	@AgentPath NVARCHAR(260)='',
	@CollectAgentID INT=NULL,
	@ImportAgentID INT=NULL,
	@contained_availability_group_id UNIQUEIDENTIFIER=NULL,
	@contained_availability_group_name NVARCHAR(128)=NULL,
	@EngineEdition INT=NULL,
	@InstanceID INT OUT,
	@IsActive BIT=NULL OUT
)
AS
DECLARE @Ref VARCHAR(30)='Instance'
DECLARE @ErrorMsg NVARCHAR(2048)

SELECT	@InstanceID = InstanceID,
		@IsActive = IsActive
FROM dbo.Instances 
WHERE ConnectionID = @ConnectionID

IF @@ROWCOUNT=0
BEGIN
	SET @IsActive=1
END

IF @SnapshotDate > DATEADD(mi,5,GETUTCDATE())
BEGIN
	SET @ErrorMsg=CONCAT('Error SnapshotDate {',@SnapshotDate,'} is greater than current UTC datetime {',GETUTCDATE(),'}') ;

	INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage,ErrorContext)
	VALUES(GETUTCDATE(),@InstanceID,'Instance',@ErrorMsg,'Import');

	THROW 50000,@ErrorMsg,1;
END
IF @IsActive=0 /* Instance is deleted, collections should not be running */
BEGIN
	SET @ErrorMsg = 'Warning: Data is being collected for an instance marked as deleted.  Remove the instance from the config tool & restart the service or use the recycle bin folder to restore the instance.  This message is logged once per hour.'
	-- Log 1 message per hour for the instance if it's marked as deleted and collections are still running
	IF NOT EXISTS(
			SELECT 1 
			FROM CollectionErrorLog
			WHERE InstanceID = @InstanceID
			AND ErrorContext = 'Import'
			AND ErrorMessage = @ErrorMsg
			AND ErrorDate >= DATEADD(mi,-60,GETUTCDATE())
	)
	BEGIN
		INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage,ErrorContext)
		VALUES(GETUTCDATE(),@InstanceID,'Instance',@ErrorMsg,'Import');
	END
END

IF @CollectAgentID IS NULL
BEGIN
	EXEC dbo.DBADashAgent_Upd
		@AgentHostName=@AgentHostName,
		@AgentVersion=@AgentVersion,
		@AgentServiceName=@AgentServiceName,
		@AgentPath=@AgentPath,
		@DBADashAgentID=@CollectAgentID OUT
END

IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	IF @InstanceID IS NULL
	BEGIN
		BEGIN TRAN
		INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,EditionID,UTCOffset,CollectAgentID,ImportAgentID,EngineEdition)
		VALUES(@Instance,@ConnectionID,CAST(1 as BIT),@EditionID,@UTCOffset,@CollectAgentID,@ImportAgentID,@EngineEdition)
		SELECT @InstanceID = SCOPE_IDENTITY();

		EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
		COMMIT

	END
	ELSE
	BEGIN
		UPDATE dbo.Instances 
		SET Instance = @Instance,
			EditionID=@EditionID,
			host_platform=@HostPlatform,
			host_distribution = @HostDistribution,
			host_release = @HostRelease,
			host_service_pack_level = @HostServicePackLevel,
			host_sku = @HostSKU,
			os_language_version = @OSLanguageVersion,
			UTCOffset = ISNULL(@UTCOffset,UTCOffset),
			CollectAgentID = @CollectAgentID,
			ImportAgentID = @ImportAgentID,
			contained_availability_group_id = @contained_availability_group_id,
			contained_availability_group_name = @contained_availability_group_name,
			EngineEdition =ISNULL(@EngineEdition,EngineEdition)
		WHERE InstanceID = @InstanceID
		AND EXISTS(SELECT Instance,
						EditionID,
						host_platform,
						host_distribution,
						host_release,
						host_service_pack_level,
						host_sku,
						os_language_version,
						UTCOffset,
						CollectAgentID,
						ImportAgentID,
						contained_availability_group_id,
						contained_availability_group_name,
						EngineEdition
					EXCEPT
					SELECT @Instance,
							@EditionID,
							@HostPlatform,
							@HostDistribution,
							@HostRelease,
							@HostServicePackLevel,
							@HostSKU,
							@OSLanguageVersion,
							ISNULL(@UTCOffset,UTCOffset),
							@CollectAgentID,
							@ImportAgentID,
							@contained_availability_group_id,
							@contained_availability_group_name,
							ISNULL(@EngineEdition,EngineEdition)
					)

		EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate

	END
END