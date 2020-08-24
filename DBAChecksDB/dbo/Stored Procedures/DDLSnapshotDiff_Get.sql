CREATE PROC [dbo].[DDLSnapshotDiff_Get](
	@DatabaseID INT,
	@SnapshotDate DATETIME2(3)
)
AS
SELECT ObjectName,
       SchemaName,
       ObjectType,
       Action,
       NewDDLID,
       OldDDLID 
FROM dbo.DBSnapshotDiff(@DatabaseID,@SnapshotDate)