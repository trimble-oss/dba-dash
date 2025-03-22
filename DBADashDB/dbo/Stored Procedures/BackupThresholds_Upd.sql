CREATE PROC BackupThresholds_Upd(
	@InstanceID INT,
	@DatabaseID INT,
	@FullWarning INT=NULL,
	@FullCritical INT=NULL,
	@DiffWarning INT=NULL,
	@DiffCritical INT=NULL,
	@LogWarning INT=NULL,
	@LogCritical INT=NULL,
	@UseFG BIT=0,
	@UsePartial BIT=0,
	@Inherit BIT=0,
	@ExcludedDatabases NVARCHAR(MAX)=NULL,
	@MinimumAge INT=NULL,
	@ConsiderSnapshotBackups BIT=1,
	@ConsiderCopyOnlyBackups BIT=1
)
AS
SET XACT_ABORT ON
BEGIN TRAN
DELETE dbo.BackupThresholds
WHERE InstanceID = @InstanceID
AND DatabaseID = @DatabaseID
IF @Inherit=0
BEGIN
INSERT INTO dbo.BackupThresholds
(
    InstanceID,
    DatabaseID,
    LogBackupWarningThreshold,
    LogBackupCriticalThreshold,
    FullBackupWarningThreshold,
    FullBackupCriticalThreshold,
    DiffBackupWarningThreshold,
    DiffBackupCriticalThreshold,
    ConsiderPartialBackups,
    ConsiderFGBackups,
	ExcludedDatabases,
	MinimumAge,
	ConsiderSnapshotBackups,
	ConsiderCopyOnlyBackups
)
VALUES(   
	@InstanceID,
	@DatabaseID,
	@LogWarning,
	@LogCritical,
	@FullWarning,
	@FullCritical,
	@DiffWarning,
	@DiffCritical,
	@UsePartial,
	@UseFG,
	@ExcludedDatabases,
	@MinimumAge,
	@ConsiderSnapshotBackups,
	@ConsiderCopyOnlyBackups
    )
END
COMMIT