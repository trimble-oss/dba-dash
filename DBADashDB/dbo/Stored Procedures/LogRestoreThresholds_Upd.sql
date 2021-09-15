CREATE PROC dbo.LogRestoreThresholds_Upd(
	@InstanceID INT,
	@DatabaseID INT,
	@LatencyWarning INT=NULL,
	@LatencyCritical INT=NULL,
	@TimeSinceLastWarning INT=NULL,
	@TimeSinceLastCritical INT=NULL,
	@Inherit BIT=0,
	@NewDatabaseExcludePeriodMin INT=1440
)
AS
SET XACT_ABORT ON
BEGIN TRAN
DELETE dbo.LogRestoreThresholds
WHERE InstanceID = @InstanceID
AND DatabaseID = @DatabaseID
IF @Inherit=0
BEGIN
INSERT INTO dbo.LogRestoreThresholds
(
    InstanceID,
    DatabaseID,
    TimeSinceLastWarningThreshold,
    TimeSinceLastCriticalThreshold,
    LatencyWarningThreshold,
	LatencyCriticalThreshold,
	NewDatabaseExcludePeriodMin
)
VALUES
(  @InstanceID,
   @DatabaseID,
   @TimeSinceLastWarning,
   @TimeSinceLastCritical,
   @LatencyWarning,
   @LatencyCritical,
   @NewDatabaseExcludePeriodMin
    )
END
COMMIT