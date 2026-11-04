CREATE PROC dbo.BusyPeriod_Del(
	@BusyPeriodID INT
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
DELETE FROM dbo.BusyPeriod
WHERE BusyPeriodID = @BusyPeriodID
