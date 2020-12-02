CREATE PROC dbo.CustomChecks_Upd(@InstanceID INT,@SnapshotDate DATETIME2(3), @CustomChecks dbo.CustomChecks READONLY)
AS
SET XACT_ABORT ON 
DECLARE @Ref NVARCHAR(128)='CustomChecks'
BEGIN TRAN
DELETE dbo.CustomChecks WHERE InstanceID=@InstanceID
INSERT INTO dbo.CustomChecks
(
    InstanceID,
    Test,
    Context,
    Status,
    Info,
	SnapshotDate
)
SELECT @InstanceID,
	   Test,
       Context,
       Status,
       Info,
	   @SnapshotDate
FROM @CustomChecks

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = @Ref, 
                             @SnapshotDate = @SnapshotDate

COMMIT TRAN