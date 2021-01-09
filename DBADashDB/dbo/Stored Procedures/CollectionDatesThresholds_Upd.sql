CREATE PROC CollectionDatesThresholds_Upd(
	@InstanceID INT,
	@Reference VARCHAR(30),
	@WarningThreshold INT,
	@CriticalThreshold INT,
	@Inherit BIT=0
)
AS
IF @Inherit=1 AND @InstanceID=-1
BEGIN
	RAISERROR('Invalid @Inherit value',11,1);
	RETURN;
END
DELETE dbo.CollectionDatesThresholds 
WHERE Reference = @Reference 
AND InstanceID = @InstanceID

IF @Inherit=0
BEGIN
	INSERT INTO dbo.CollectionDatesThresholds
	(
		InstanceID,
		Reference,
		WarningThreshold,
		CriticalThreshold
	)
	VALUES
	(   @InstanceID, 
		@Reference, 
		@WarningThreshold, 
		@CriticalThreshold 
		)
END