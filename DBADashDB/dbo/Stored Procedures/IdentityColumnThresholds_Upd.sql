CREATE PROC dbo.IdentityColumnThresholds_Upd(
	@InstanceID INT,
	@DatabaseID INT,
	@object_name NVARCHAR(128),
	@PctUsedWarningThreshold  DECIMAL(9,3),
	@PctUsedCriticalThreshold DECIMAL(9,3),
	@Inherit BIT = 0
)
AS
SET XACT_ABORT ON
BEGIN TRAN
DELETE T 
FROM dbo.IdentityColumnThresholds T
WHERE T.InstanceID = @InstanceID
AND T.DatabaseID = @DatabaseID
AND T.object_name = @object_name

IF @Inherit=0
BEGIN
	INSERT INTO dbo.IdentityColumnThresholds(InstanceID,DatabaseID,object_name,PctUsedWarningThreshold,PctUsedCriticalThreshold)
	VALUES(@InstanceID,@DatabaseID,@object_name,@PctUsedWarningThreshold,@PctUsedCriticalThreshold)
END

COMMIT