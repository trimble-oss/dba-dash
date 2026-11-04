CREATE PROC dbo.BusyPeriods_Copy(
	@FromInstanceID INT,
	@ToInstanceID INT
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
IF @FromInstanceID = @ToInstanceID
BEGIN
	RAISERROR('Source and target instance must be different',11,1)
	RETURN
END
/* Replaces the target instance's own busy periods with copies of the source instance's own periods.
   Tag and all-instance periods are not copied, as they already apply to the target if they match it.
   The delete and insert run in one transaction, so a failure leaves the target unchanged. */
BEGIN TRANSACTION

DELETE FROM dbo.BusyPeriod
WHERE ApplyToInstanceID = @ToInstanceID

INSERT INTO dbo.BusyPeriod(ApplyToInstanceID, ApplyToTagID, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday, TimeFrom, TimeTo, TimeZone, Description)
SELECT @ToInstanceID, -1, BP.Monday, BP.Tuesday, BP.Wednesday, BP.Thursday, BP.Friday, BP.Saturday, BP.Sunday, BP.TimeFrom, BP.TimeTo, BP.TimeZone, BP.Description
FROM dbo.BusyPeriod BP
WHERE BP.ApplyToInstanceID = @FromInstanceID

COMMIT
