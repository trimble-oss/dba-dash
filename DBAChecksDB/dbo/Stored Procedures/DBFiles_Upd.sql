
CREATE PROC [dbo].[DBFiles_Upd](
	@Files DBFiles READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME
)
AS
WITH T AS (
	SELECT F.* 
	FROM dbo.Databases D
	JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
	JOIN dbo.DBFiles F ON F.DatabaseID = D.DatabaseID
	WHERE I.InstanceID = @InstanceID
)
MERGE T
USING (SELECT D.DatabaseID
      ,F.[file_id]
      ,F.[data_space_id]
      ,F.[name]
      ,F.[filegroup_name]
      ,F.[physical_name]
      ,F.[type]
      ,F.[size]
      ,F.[space_used]
      ,F.[max_size]
      ,F.[growth]
      ,F.[is_percent_growth]
      ,F.[is_read_only] 
	  FROM @Files F 
	  JOIN dbo.Databases D ON F.database_id = D.database_id
	  WHERE D.IsActive=1
	  AND D.InstanceID = @InstanceID) AS S ON S.file_id = T.file_id AND S.DatabaseID = T.DatabaseID
WHEN MATCHED THEN 
 UPDATE 
 SET T.[data_space_id]=S.[data_space_id]
      ,T.[name]=S.[name]
      ,T.[filegroup_name]=S.[filegroup_name]
      ,T.[physical_name]=S.[physical_name]
      ,T.[type]=S.[type]
      ,T.[size]=S.[size]
      ,T.[space_used]=S.[space_used]
      ,T.[max_size]=S.[max_size]
      ,T.[growth]=S.[growth]
      ,T.[is_percent_growth]=S.[is_percent_growth]
      ,T.[is_read_only] =S.[is_read_only] 
	  ,T.IsActive=1
WHEN NOT MATCHED BY TARGET THEN
INSERT      (DatabaseID
           ,[file_id]
           ,[data_space_id]
           ,[name]
           ,[filegroup_name]
           ,[physical_name]
           ,[type]
           ,[size]
           ,[space_used]
           ,[max_size]
           ,[growth]
           ,[is_percent_growth]
           ,[is_read_only]
		   ,IsActive)
VALUES(DatabaseID
      ,[file_id]
      ,[data_space_id]
      ,[name]
      ,[filegroup_name]
      ,[physical_name]
      ,[type]
      ,[size]
      ,[space_used]
      ,[max_size]
      ,[growth]
      ,[is_percent_growth]
      ,[is_read_only]
	  ,1)
WHEN NOT MATCHED BY SOURCE THEN
UPDATE SET T.IsActive=0;

INSERT INTO dbo.DBFileSnapshot(SnapshotDate,FileID,Size,space_used)
SELECT @SnapshotDate,
	  DBF.FileID,
      F.[size],
      F.[space_used]
FROM @Files F 
JOIN dbo.Databases D ON F.database_id = D.database_id AND D.InstanceID = @InstanceID
JOIN dbo.DBFiles DBF ON F.file_id = DBF.file_id AND D.DatabaseID = DBF.DatabaseID

UPDATE dbo.SnapshotDates
SET DBFilesDate=@SnapshotDate
WHERE InstanceID=@InstanceID