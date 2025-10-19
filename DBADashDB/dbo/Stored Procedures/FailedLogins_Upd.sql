CREATE PROCEDURE dbo.FailedLogins_Upd(
	@InstanceID INT,
	@SnapshotDate DATETIME2,
	@FailedLogins dbo.FailedLogins READONLY
)
AS
SET XACT_ABORT ON
SET NOCOUNT ON

DECLARE @MaxLogDate DATETIME2

SELECT @MaxLogDate = ISNULL(MAX(LogDate),'19000101')
FROM dbo.FailedLogins
WHERE InstanceID = @InstanceID


INSERT INTO dbo.FailedLogins(
	InstanceID,
	LogDate,
	Text,
	Uniqueifier
)
SELECT	@InstanceID AS InstanceID,
		LogDate,
		[Text],
		ROW_NUMBER() OVER (PARTITION BY LogDate ORDER BY LogDate) AS Uniqueifier /* Just in case we have duplicate rows with same LogDate */
FROM @FailedLogins
WHERE LogDate > @MaxLogDate


/* Log the data collection */
EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
		@Reference = 'FailedLogins',
		@SnapshotDate = @SnapshotDate
GO