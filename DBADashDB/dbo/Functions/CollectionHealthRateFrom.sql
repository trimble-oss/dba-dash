/*
	The start of the period the summary rates each collection's failures over (dbo.Summary_Upd) - since the errors were last
	acknowledged, 24hrs at most.  The rate is read from the hourly rollup, so it starts at the next whole hour.  Shared with the
	Collection Health report's Failed % 24hrs column (dbo.CollectionHealth_Get) so the two agree.
*/
CREATE FUNCTION dbo.CollectionHealthRateFrom()
RETURNS TABLE
AS
RETURN
SELECT	CAST(CASE WHEN DATEADD(hh,DATEDIFF(hh,0,W.ErrorsFrom),0) < W.ErrorsFrom
				THEN DATEADD(hh,DATEDIFF(hh,0,W.ErrorsFrom) + 1,0)
				ELSE DATEADD(hh,DATEDIFF(hh,0,W.ErrorsFrom),0) END AS DATETIME2(2)) AS RateFrom
FROM (	SELECT CASE WHEN A.AckDate > DATEADD(d,-1,GETUTCDATE()) THEN A.AckDate ELSE DATEADD(d,-1,GETUTCDATE()) END AS ErrorsFrom
		FROM (	SELECT MAX(CONVERT(DATETIME,SettingValue)) AS AckDate
				FROM dbo.Settings
				WHERE SettingName = 'ErrorAckDate'
				) A
		) W
