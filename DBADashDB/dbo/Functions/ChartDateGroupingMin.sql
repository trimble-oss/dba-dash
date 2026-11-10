/*
	The chart bucket (minutes) for a date range - the smallest that keeps a chart to fewer than @MaxPoints points, as the GUI's
	DateHelper.DateGrouping picks for the CPU, IO and other performance charts (200 points).  An hour's range is in 1 minute
	buckets, a day in 10, a week by the hour.  The same choice of buckets as the GUI's date grouping menu.
*/
CREATE FUNCTION dbo.ChartDateGroupingMin(
	@RangeMins INT,
	@MaxPoints INT
)
RETURNS TABLE
AS
RETURN
SELECT	ISNULL((	SELECT TOP(1) G.Mins
				FROM (VALUES(1),(2),(5),(10),(15),(30),(60),(120),(240),(360),(720),(1440),(2880),(10080)) G(Mins)
				WHERE @RangeMins * 1.0 / G.Mins < @MaxPoints
				ORDER BY G.Mins
				),10080) AS DateGroupingMin
