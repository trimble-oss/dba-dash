CREATE PROC [dbo].[Instance_Upd](
	@ConnectionID SYSNAME,
	@Instance SYSNAME,
	@SnapshotDate DATETIME2(2),
	@AgentHostName NVARCHAR(16),
	@AgentVersion VARCHAR(30)=NULL,
	@EditionID BIGINT=NULL,
	@HostPlatform NVARCHAR(256)=NULL,
	@HostDistribution NVARCHAR(256)=NULL,
	@HostRelease NVARCHAR(256)=NULL,
	@HostServicePackLevel NVARCHAR(256)=NULL,
	@HostSKU INT=NULL,
	@OSLanguageVersion INT=NULL,
	@UTCOffset INT=NULL,
	@InstanceID INT OUT
)
AS
SELECT @InstanceID = InstanceID
FROM dbo.Instances 
WHERE ConnectionID = @ConnectionID

DECLARE @Ref VARCHAR(30)='Instance'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	IF @InstanceID IS NULL
	BEGIN
		BEGIN TRAN
		INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,AgentHostName,AgentVersion,EditionID,UTCOffset)
		VALUES(@Instance,@ConnectionID,CAST(1 as BIT),@AgentHostName,@AgentVersion,@EditionID,@UTCOffset)
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
			AgentHostName=@AgentHostName,
			AgentVersion=@AgentVersion,
			EditionID=@EditionID,
			host_platform=@HostPlatform,
			host_distribution = @HostDistribution,
			host_release = @HostRelease,
			host_service_pack_level = @HostServicePackLevel,
			host_sku = @HostSKU,
			os_language_version = @OSLanguageVersion,
			UTCOffset = ISNULL(@UTCOffset,UTCOffset)
		WHERE InstanceID = @InstanceID
		AND EXISTS(SELECT Instance,
						AgentHostName,
						AgentVersion,
						EditionID,
						host_platform,
						host_distribution,
						host_release,
						host_service_pack_level,
						host_sku,
						os_language_version,
						UTCOffset
					EXCEPT
					SELECT @Instance,
							@AgentHostName,
							@AgentVersion,
							@EditionID,
							@HostPlatform,
							@HostDistribution,
							@HostRelease,
							@HostServicePackLevel,
							@HostSKU,
							@OSLanguageVersion,
							ISNULL(@UTCOffset,UTCOffset)
					)
		AND @HostPlatform IS NOT NULL -- for older agent version

		EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	END
END