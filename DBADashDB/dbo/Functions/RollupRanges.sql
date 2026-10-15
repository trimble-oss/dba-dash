/*
	Where a window divides between the hourly rollup and the raw rows it is built from, for a caller that reads
	whole hours from the one and the part hours at either end from the other.  An hour in a rollup is exactly the
	raw rows whose snapshot falls in it, so the three ranges together give the raw rows' figures for close to the
	cost of reading the rollup alone:

		[@From, HourFrom)          raw rows
		[HourFrom, HourTo)         hourly rollup
		[HourTo, @ToExclusive)     raw rows

	HourFrom is the start of the first hour inside the window and HourTo the end of the last.  A window with no
	whole hour inside it is read from the raw rows, as the first range, and the other two are empty.

	@UseRollup NULL splits the window as above.  0 reads it all from the raw rows and 1 all from the rollup - a
	rollup only read of a window that doesn't start and end on the hour leaves out the part hours.

	For assigning to variables, not for joining to: each range is read as its own seek on a pair of variables.
	See dbo.QueryStats_Get for why the ranges are not rows of a table the read joins to.
*/
CREATE FUNCTION dbo.RollupRanges(
	@From DATETIME2(3),
	@ToExclusive DATETIME2(3),
	@UseRollup BIT
)
RETURNS TABLE
AS
RETURN
SELECT HourFrom = CASE WHEN @UseRollup = 1 THEN @From
					   WHEN @UseRollup = 0 OR H.HourFrom >= H.HourTo THEN @ToExclusive
					   ELSE H.HourFrom END,
	   HourTo = CASE WHEN @UseRollup = 1 THEN @ToExclusive
					 WHEN @UseRollup = 0 OR H.HourFrom >= H.HourTo THEN @ToExclusive
					 ELSE H.HourTo END
FROM (SELECT HourFrom = CONVERT(DATETIME2(3), CASE WHEN F.DateGroup < @From THEN DATEADD(HOUR, 1, F.DateGroup) ELSE F.DateGroup END),
			 HourTo = CONVERT(DATETIME2(3), T.DateGroup)
	  FROM dbo.DateGroupingMins(@From, 60) F
	  CROSS JOIN dbo.DateGroupingMins(@ToExclusive, 60) T) H
