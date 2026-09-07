/*
	Truncates a time to the start of the @Mins minute bucket it falls in, so that a chart or a rollup
	can group by it.  @Mins = 0 passes the time through ungrouped.

	Takes DATETIME2(7) rather than the DATETIME it originally took.  Every column passed to it is
	DATETIME2(2) or DATETIME2(3), and DATETIME cannot hold every millisecond - it rounds to 1/300th of
	a second - so a DATETIME2 argument was rounded on the way in.  A bucketed result absorbed that,
	being a whole minute, except where the rounding carried a time across a minute boundary and into
	the next bucket.  An ungrouped result returned it, which handed the caller a time that never
	happened: an event at .996 came back as .997, and dbo.DeadlockCharts_Get sent that value back as an
	inclusive lower bound, excluding the very event the chart point counted.

	DATETIME2(7) takes DATETIME, SMALLDATETIME and any DATETIME2 scale without loss, so what a caller
	passes no longer changes what it gets back.

	The buckets are still measured from 1900-01-01, which is what the epoch of 0 meant when this was
	written against DATETIME - so boundaries are unchanged and existing aggregates still line up.
*/
CREATE FUNCTION dbo.DateGroupingMins(@DateTime DATETIME2(7),@Mins INT)
RETURNS TABLE
AS
RETURN
SELECT CASE WHEN @Mins = 0 THEN @DateTime
			ELSE DATEADD(MINUTE,
					(DATEDIFF(MINUTE, CONVERT(DATETIME2(7),'19000101'), @DateTime) / @Mins) * @Mins,
					CONVERT(DATETIME2(7),'19000101'))
			END AS DateGroup
