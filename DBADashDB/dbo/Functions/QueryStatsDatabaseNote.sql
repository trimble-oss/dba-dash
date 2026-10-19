/*
	What the query stats reports say when they are filtered to one database and the figures may not be all of its
	work.  Shared by dbo.QueryStats_Get and dbo.QueryStatsCharts_Get so the grid and the charts say the same thing
	about the same window.  NULL where there is nothing to say.

	The collection keeps an {Other queries} rollup per database for the top N databases only, and combines the rest
	into one {Other databases} row with no database - so a database filter leaves it out, and a database that was
	past the cap for the whole window has no rows at all.  The collection does not record which databases went into
	it, so the note says may.

	@HasWork is whether the database has any rows in the window, named or rolled up, so an empty report says why it
	is empty rather than looking broken.
*/
CREATE FUNCTION dbo.QueryStatsDatabaseNote(
	@HasWork BIT,
	@OtherDatabasesWorkerTime BIGINT,
	@InstanceWorkerTime BIGINT
)
RETURNS NVARCHAR(500)
AS
BEGIN
	DECLARE @Empty NVARCHAR(100) = CASE WHEN @HasWork = 0 THEN N'Nothing was collected for this database in this window.  ' ELSE N'' END

	IF @OtherDatabasesWorkerTime > 0
		RETURN @Empty
			+ CONVERT(NVARCHAR(20), CAST(100.0 * @OtherDatabasesWorkerTime / NULLIF(@InstanceWorkerTime, 0) AS DECIMAL(5, 1)))
			+ N'% of the instance''s CPU was rolled up into {Other databases} - the databases past Query Stats Top N, combined - and some of this database''s work may be in it.  Raise Query Stats Top N on the connection to keep it.'

	RETURN NULLIF(RTRIM(@Empty), N'')
END
