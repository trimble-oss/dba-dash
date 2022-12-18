﻿CREATE PROC dbo.LastGoodCheckDB_Get(
        @InstanceIDs VARCHAR(MAX)=NULL,
        @IncludeCritical BIT=1,
        @IncludeWarning BIT=1,
        @IncludeNA BIT=0,
        @IncludeOK BIT=0,
        @ShowHidden BIT=1
)
AS
DECLARE @StatusSQL NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)
SELECT @StatusSQL = CASE WHEN @IncludeCritical=1 THEN ',1' ELSE '' END
	+ CASE WHEN @IncludeWarning=1 THEN ',2' ELSE '' END
	+ CASE WHEN @IncludeNA=1 THEN ',3' ELSE '' END
	+ CASE WHEN @IncludeOK=1 THEN ',4' ELSE '' END

SELECT @StatusSQL = CASE WHEN @StatusSQL='' THEN 'AND 1=2'
		ELSE 'AND LG.Status IN(' + STUFF(@StatusSQL,1,1,'') + ')' END

SET @SQL = N'
SELECT InstanceID,
		DatabaseID,
       Instance,
       InstanceDisplayName,
       Name,
       state,
       state_desc,
       is_in_standby,
       LastGoodCheckDbTimeUTC,
       HrsSinceLastGoodCheckDB,
       DaysSinceLastGoodCheckDB,
       Status,
       StatusDescription,
       ConfiguredLevel,
       WarningThresholdHrs,
       CriticalThresholdHrs,
       LastGoodCheckDBExcludedReason,
       create_date_utc
FROM dbo.LastGoodCheckDB LG
WHERE 1=1' + 
CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS (SELECT 1
			FROM STRING_SPLIT(@InstanceIDs,'','') ss
			WHERE ss.value = LG.InstanceID
			)' END + '
' + @StatusSQL + '
' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND LG.ShowInSummary=1' END

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX)',@InstanceIDs