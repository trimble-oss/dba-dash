/*	How long ago, for daily report text: "45 minutes", "5 hours", "3 days".  Minutes up to 2 hours and hours up to 2 days, so a warning
	on a threshold of a few hours doesn't read "0 days ago" */
CREATE FUNCTION dbo.DailyReport_AgeText(
	@From DATETIME2(3),
	@To DATETIME2(3)
)
RETURNS TABLE
AS
RETURN
SELECT CASE WHEN A.Mins < 120 THEN CONCAT(A.Mins, CASE WHEN A.Mins = 1 THEN ' minute' ELSE ' minutes' END)
			WHEN A.Mins < 2880 THEN CONCAT(A.Mins/60, ' hours')
			ELSE CONCAT(A.Mins/1440, ' days') END AS AgeText
FROM (SELECT DATEDIFF_BIG(mi, @From, @To) AS Mins) A
