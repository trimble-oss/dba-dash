CREATE PROC dbo.DDLSnapshotDates_Get(
	@DatabaseID INT,
	@Top INT=100
)
AS
SELECT TOP(@Top) SnapshotDate,ValidatedDate
FROM dbo.DDLSnapshots
WHERE DatabaseID = @DatabaseID
ORDER BY ValidatedDate DESC