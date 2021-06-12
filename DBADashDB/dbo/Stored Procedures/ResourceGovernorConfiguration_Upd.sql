CREATE PROC dbo.ResourceGovernorConfiguration_Upd(
	@ResourceGovernorConfiguration dbo.ResourceGovernorConfiguration READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='ResourceGovernorConfiguration'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	UPDATE RGC 
		SET RGC.ValidTo = @SnapshotDate
	FROM dbo.ResourceGovernorConfigurationHistory RGC
	WHERE InstanceID = @InstanceID
	AND ValidTo = '9999-12-31'
	AND EXISTS(SELECT is_enabled,
					  classifier_function,
					  reconfiguration_error,
					  reconfiguration_pending,
					  max_outstanding_io_per_volume,
					  script FROM @ResourceGovernorConfiguration
				EXCEPT 
				SELECT is_enabled,
							classifier_function,
							reconfiguration_error,
							reconfiguration_pending,
							max_outstanding_io_per_volume,
							script
			)

	INSERT INTO dbo.ResourceGovernorConfigurationHistory
	(
		InstanceID,
		is_enabled,
		classifier_function,
		reconfiguration_error,
		reconfiguration_pending,
		max_outstanding_io_per_volume,
		script,
		ValidFrom,
		ValidTo
	)
	SELECT @InstanceID,
		is_enabled,
		classifier_function,
		reconfiguration_error,
		reconfiguration_pending,
		max_outstanding_io_per_volume,
		script,
		@SnapshotDate ValidFrom,
		'9999-12-31' AS ValidTo	
	FROM @ResourceGovernorConfiguration
	WHERE NOT EXISTS(SELECT 1 
					FROM dbo.ResourceGovernorConfigurationHistory RGC 
					WHERE RGC.InstanceID = @InstanceID 
					AND RGC.ValidTo = '9999-12-31'
					);

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate;

	COMMIT
END