CREATE PROC dbo.DDLSnapshots_Get(
	@InstanceDisplayName NVARCHAR(128),
	@DatabaseID INT=-1,
	@PageSize INT=100,
	@PageNumber INT=1
)
AS
SELECT I.InstanceGroupName,
       ss.DatabaseID,
       ss.SnapshotDate,
       ss.ValidatedDate,
	   DATEDIFF(d,ss.SnapshotDate,ss.ValidatedDate) AS ValidForDays,
	   DATEDIFF(d,ss.ValidatedDate,GETUTCDATE()) AS DaysSinceValidation,
       ss.Created,
       ss.Modified,
       ss.Dropped,
       ss.DDLSnapshotOptionsID,
        D.name as DB
FROM dbo.DDLSnapshots ss
JOIN dbo.Databases D ON ss.DatabaseID = D.DatabaseID
JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
WHERE (d.DatabaseID=@DatabaseID OR @DatabaseID =-1)
AND I.InstanceDisplayName = @InstanceDisplayName
ORDER BY ss.SnapshotDate DESC
OFFSET @PageSize* (@PageNumber-1) ROWS
FETCH NEXT @PageSize ROWS ONLY
OPTION(RECOMPILE)