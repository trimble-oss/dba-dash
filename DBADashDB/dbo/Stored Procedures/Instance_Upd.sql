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
	@InstanceID INT OUT
)
AS
DECLARE @Ref VARCHAR(30)='Instance'

SELECT @InstanceID = InstanceID
FROM dbo.Instances 
WHERE ConnectionID = @ConnectionID

IF @SnapshotDate > DATEADD(mi,5,GETUTCDATE())
BEGIN
	DECLARE @ErrorMsg NVARCHAR(2048)
	SET @ErrorMsg=CONCAT('Error SnapshotDate {',@SnapshotDate,'} is greater than current UTC datetime {',GETUTCDATE(),'}') ;

	INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID,ErrorSource,ErrorMessage,ErrorContext)
	VALUES(GETUTCDATE(),@InstanceID,'Instance',@ErrorMsg,'Import');

	THROW 50000,@ErrorMsg,1;
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
		INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,EditionID,UTCOffset,CollectAgentID,ImportAgentID)
		VALUES(@Instance,@ConnectionID,CAST(1 as BIT),@EditionID,@UTCOffset,@CollectAgentID,@ImportAgentID)
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
			ImportAgentID = @ImportAgentID
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
						ImportAgentID
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
							@ImportAgentID
					)
		AND @HostPlatform IS NOT NULL -- for older agent version

		EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	END
END