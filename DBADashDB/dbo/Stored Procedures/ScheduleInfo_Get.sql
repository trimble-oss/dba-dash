CREATE PROC dbo.ScheduleInfo_Get(
	@InstanceIDs IDs READONLY, 
	@Reference VARCHAR(MAX),
	@IsEnabled BIT = NULL
)
AS
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT	SI.InstanceID,
		SI.Reference,
		SI.Schedule,
		SI.RunOnServiceStart, 
		SI.MaxIntervalMinutes,
		SI.IsInstanceOverride,
		SI.SnapshotDate,
		SI.IsEnabled
FROM dbo.ScheduleInfo SI
WHERE EXISTS(SELECT 1 
			FROM @InstanceIDs I 
			WHERE I.ID = SI.InstanceID
			)
' + CASE WHEN @Reference IS NULL THEN '' 
	ELSE 'AND EXISTS(SELECT 1 
		FROM STRING_SPLIT(@Reference,'','') R 
		WHERE R.value = SI.Reference)
		' END  + '
' + CASE WHEN @IsEnabled IS NULL THEN '' 
	ELSE 'AND SI.IsEnabled = @IsEnabled' END

EXEC sp_executesql @SQL,N'@InstanceIDs IDs READONLY,@Reference VARCHAR(MAX),@IsEnabled BIT',@InstanceIDs,@Reference,@IsEnabled