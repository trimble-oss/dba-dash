/*
	Decodes the two SET option bitmasks carried on a deadlock graph's process element into readable
	option lists, the way sp_BlitzLock does.

	clientoption1 is the session's SET options - @@OPTIONS - so this is where ARITHABORT,
	IMPLICIT_TRANSACTIONS, XACT_ABORT and friends show up, and those are frequently the answer to
	"why did this deadlock here and not when I ran it".  clientoption2 is the database options the
	session inherited.

	The names and bit values are the ones SQL Server itself publishes in master.dbo.spt_values under
	type 'SOP' (clientoption1) and type 'D2' (clientoption2).  They are written out here rather than
	read from that table: the repository can be an Azure SQL Database, where a user database cannot
	reach master, and the project's master reference is the Azure master, which has no spt_values.
	The values are fixed - they have not changed across supported versions.

	Two rows in spt_values are deliberately not represented.  Each type has a number = 0 header row
	('@@OPTIONS', 'DATABASE OPTIONS') which is a label rather than a bit, and 'D2' carries a
	1469283328 rollup ('ALL SETTABLE OPTIONS') which is every other bit ORed together - it only ever
	matches when all of those options are already listed individually, so including it would add a
	line that says nothing.

	Bits outside these lists are ignored rather than reported as unknown.  Real clientoption1 values
	carry bits above 16384 that SQL Server does not name, and listing them as numbers would be noise.

	Returns NULL for a NULL input (the graph did not carry the attribute) and an empty string when the
	mask is set but no named option is on - which are different things and worth telling apart.
*/
CREATE FUNCTION dbo.DecodeClientOptions(
	@ClientOption1 INT,
	@ClientOption2 INT
)
RETURNS TABLE
AS
RETURN
SELECT	CASE WHEN @ClientOption1 IS NULL THEN NULL ELSE
			SUBSTRING(
				CASE WHEN @ClientOption1 & 1 = 1 THEN ', DISABLE_DEF_CNST_CHECK' ELSE '' END +
				CASE WHEN @ClientOption1 & 2 = 2 THEN ', IMPLICIT_TRANSACTIONS' ELSE '' END +
				CASE WHEN @ClientOption1 & 4 = 4 THEN ', CURSOR_CLOSE_ON_COMMIT' ELSE '' END +
				CASE WHEN @ClientOption1 & 8 = 8 THEN ', ANSI_WARNINGS' ELSE '' END +
				CASE WHEN @ClientOption1 & 16 = 16 THEN ', ANSI_PADDING' ELSE '' END +
				CASE WHEN @ClientOption1 & 32 = 32 THEN ', ANSI_NULLS' ELSE '' END +
				CASE WHEN @ClientOption1 & 64 = 64 THEN ', ARITHABORT' ELSE '' END +
				CASE WHEN @ClientOption1 & 128 = 128 THEN ', ARITHIGNORE' ELSE '' END +
				CASE WHEN @ClientOption1 & 256 = 256 THEN ', QUOTED_IDENTIFIER' ELSE '' END +
				CASE WHEN @ClientOption1 & 512 = 512 THEN ', NOCOUNT' ELSE '' END +
				CASE WHEN @ClientOption1 & 1024 = 1024 THEN ', ANSI_NULL_DFLT_ON' ELSE '' END +
				CASE WHEN @ClientOption1 & 2048 = 2048 THEN ', ANSI_NULL_DFLT_OFF' ELSE '' END +
				CASE WHEN @ClientOption1 & 4096 = 4096 THEN ', CONCAT_NULL_YIELDS_NULL' ELSE '' END +
				CASE WHEN @ClientOption1 & 8192 = 8192 THEN ', NUMERIC_ROUNDABORT' ELSE '' END +
				CASE WHEN @ClientOption1 & 16384 = 16384 THEN ', XACT_ABORT' ELSE '' END,
				3,
				500)
		END AS ClientOption1Description,
		CASE WHEN @ClientOption2 IS NULL THEN NULL ELSE
			SUBSTRING(
				CASE WHEN @ClientOption2 & 1024 = 1024 THEN ', DB CHAINING' ELSE '' END +
				CASE WHEN @ClientOption2 & 2048 = 2048 THEN ', NUMERIC ROUNDABORT' ELSE '' END +
				CASE WHEN @ClientOption2 & 4096 = 4096 THEN ', ARITHABORT' ELSE '' END +
				CASE WHEN @ClientOption2 & 8192 = 8192 THEN ', ANSI PADDING' ELSE '' END +
				CASE WHEN @ClientOption2 & 16384 = 16384 THEN ', ANSI NULL DEFAULT' ELSE '' END +
				CASE WHEN @ClientOption2 & 65536 = 65536 THEN ', CONCAT NULL YIELDS NULL' ELSE '' END +
				CASE WHEN @ClientOption2 & 131072 = 131072 THEN ', RECURSIVE TRIGGERS' ELSE '' END +
				CASE WHEN @ClientOption2 & 1048576 = 1048576 THEN ', DEFAULT TO LOCAL CURSOR' ELSE '' END +
				CASE WHEN @ClientOption2 & 8388608 = 8388608 THEN ', QUOTED IDENTIFIER' ELSE '' END +
				CASE WHEN @ClientOption2 & 16777216 = 16777216 THEN ', AUTO CREATE STATISTICS' ELSE '' END +
				CASE WHEN @ClientOption2 & 33554432 = 33554432 THEN ', CURSOR CLOSE ON COMMIT' ELSE '' END +
				CASE WHEN @ClientOption2 & 67108864 = 67108864 THEN ', ANSI NULLS' ELSE '' END +
				CASE WHEN @ClientOption2 & 268435456 = 268435456 THEN ', ANSI WARNINGS' ELSE '' END +
				CASE WHEN @ClientOption2 & 536870912 = 536870912 THEN ', FULL TEXT ENABLED' ELSE '' END +
				CASE WHEN @ClientOption2 & 1073741824 = 1073741824 THEN ', AUTO UPDATE STATISTICS' ELSE '' END,
				3,
				500)
		END AS ClientOption2Description
