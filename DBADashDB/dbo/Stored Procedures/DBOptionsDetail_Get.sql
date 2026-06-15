CREATE PROC dbo.DBOptionsDetail_Get(
	@InstanceIDs IDs READONLY,
	@DatabaseID INT = NULL,
	@ShowHidden BIT = 1,
	@InstanceGroupName NVARCHAR(128) = NULL,
	@ExcludeStateChanges BIT = 1
)
AS
DECLARE @InstanceIDsString VARCHAR(MAX)
SET @InstanceIDsString = STUFF(
			(
			SELECT CONCAT(',',ID) 
			FROM @InstanceIDs
			FOR XML PATH(''),TYPE
			).value('.','VARCHAR(MAX)')
		,1,1,'')

-- Result Set 1: Detail data (per-database options)
EXEC dbo.DatabasesAllInfo_Get
	@InstanceIDs = @InstanceIDsString,
	@DatabaseID = @DatabaseID,
	@ShowHidden = @ShowHidden,
	@InstanceGroupName = @InstanceGroupName

-- Result Set 2: History data
EXEC dbo.DBOptionsHistory_Get
	@InstanceIDs = @InstanceIDsString,
	@DatabaseID = @DatabaseID,
	@ExcludeStateChanges = @ExcludeStateChanges,
	@ShowHidden = @ShowHidden,
	@InstanceGroupName = @InstanceGroupName
