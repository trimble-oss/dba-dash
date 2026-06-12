CREATE PROC dbo.DBOptionsSummary_Get(
	@InstanceIDs IDs READONLY,
	@DatabaseID INT = NULL,
	@ShowHidden BIT = 1,
	@ExcludeStateChanges BIT = 1
)
AS
DECLARE @InstanceIDsString VARCHAR(MAX)
SELECT @InstanceIDsString = STRING_AGG(CAST(ID AS VARCHAR(20)), ',') FROM @InstanceIDs

-- Result Set 1: Summary data (instance-level aggregates)
EXEC dbo.DBSummary_Get
	@InstanceIDs = @InstanceIDsString,
	@ShowHidden = @ShowHidden

-- Result Set 2: History data
EXEC dbo.DBOptionsHistory_Get
	@InstanceIDs = @InstanceIDsString,
	@DatabaseID = @DatabaseID,
	@ExcludeStateChanges = @ExcludeStateChanges,
	@ShowHidden = @ShowHidden
