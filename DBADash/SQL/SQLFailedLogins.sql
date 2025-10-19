/*
DECLARE @FailedLoginsBackfillMinutes INT = 1440; -- last 24 hours
*/
DECLARE @CollectFromDate DATETIME
SET @CollectFromDate = DATEADD(mi,-@FailedLoginsBackfillMinutes,GETDATE()) 

CREATE TABLE #ErrorLog(
	LogDate DATETIME,
	ProcessInfo NVARCHAR(50),
	Text NVARCHAR(MAX)
)
CREATE TABLE #enum(
        ArchiveNumber INT PRIMARY KEY,
        LogDate DATETIME,
        LogSize BIGINT
    );
INSERT INTO #enum
EXEC sys.sp_enumerrorlogs;

DECLARE @ArchiveNumber INT
DECLARE c1 CURSOR FAST_FORWARD LOCAL FOR SELECT ArchiveNumber
					FROM #enum
					WHERE LogDate >= @CollectFromDate
OPEN c1
WHILE 1=1
BEGIN
	FETCH NEXT FROM c1 INTO @ArchiveNumber
	IF @@FETCH_STATUS<>0
		BREAK
	INSERT INTO #ErrorLog
	EXEC sp_readerrorlog @ArchiveNumber, 1, 'Login failed' 
END

CLOSE c1
DEALLOCATE c1

SELECT	DATEADD(mi,DATEDIFF(mi,GETDATE(),GETUTCDATE()),LogDate) AS LogDate, /* Convert to UTC based on current offset */
		Text
FROM #ErrorLog
WHERE LogDate >= @CollectFromDate

DROP TABLE #ErrorLog
DROP TABLE #enum