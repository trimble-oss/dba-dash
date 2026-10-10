/*
	The text a stored statement is shown with, and a short label for it.

	For most statements that is the statement cut out of the batch text it belongs to.  The offsets stored
	against a statement are byte offsets into an NVARCHAR batch, and an end offset of -1 means "to the end",
	which is why this is not a plain SUBSTRING.  The same arithmetic the running queries view uses, so a
	statement reads identically wherever it is shown.

	An ad hoc shape (StatementType 5) is shown as its template instead: its example's statement with the
	literal values and comments taken out, which is what the variants behind it differ in.  Never as the
	example itself, so a shape whose template has not arrived yet shows nothing rather than one variant's
	values - the example is still a click away, labelled as one, through dbo.QueryStatementBatchText_Get.

	The label is not read from here by the reports.  QueryStats_Upd stores it as dbo.QueryStatements.StatementLabel
	when the text arrives, because flattening the text is slow enough to dominate a report that labels a few
	thousand statements, and the label only changes when the text does.  The reports read the stored column, so
	the charts report and the grid report that filters on its series names agree by reading the same value.

	Inline table valued, used through APPLY, so it folds into the calling query rather than being called per
	row like a scalar function would be - and the batch it looks up is only looked up for the statements that
	need one.
*/
CREATE FUNCTION dbo.QueryStatementText (
	@sql_handle VARBINARY(64),
	@StartOffset INT,
	@EndOffset INT,
	@StatementType TINYINT,
	@StatementTemplate NVARCHAR(MAX)
)
RETURNS TABLE
AS
RETURN
SELECT StatementText = F.StatementText,
	   /*
			A legend entry, not the statement.  Every character that formats SQL rather than saying anything
			becomes a space, and runs of spaces collapse to one, so a statement written across six indented
			lines reads as one line of words.  Carriage return, line feed and tab are the three that matter;
			a tab left in place renders as a missing-glyph box in a chart legend, which is what gave this
			away.  Then cut to a length that fits a tooltip or a legend-less chart: sixty characters is most
			of a SELECT list and none of the FROM or WHERE, so statements that differ only past that point
			would read identically - which is exactly the case where a reader is trying to tell two apart.
			The sixty character cut, for a legend or a grouped grid row, is StatementLabelShort on the table.
	   */
	   Label = NULLIF(LEFT(F.Flat, 160), '') COLLATE DATABASE_DEFAULT
FROM (
	SELECT T.StatementText,
		   /*
				The collapse is the standard three-REPLACE trick: mark every space, delete the marks that met
				another mark, then turn what is left back into a space.  The markers are control characters
				rather than punctuation so that text containing the markers cannot be mangled.

				Done on the first 4000 characters only, and in a binary collation.  The labels keep at most 160
				characters, so flattening the rest of a long statement is work thrown away - and on NVARCHAR(MAX)
				with a linguistic collation REPLACE is slow enough to dominate any report that labels a few
				hundred statements, more so because the expression is inlined and evaluated once per reference.
				Binary is also the exact comparison wanted here: a linguistic collation can give control
				characters no weight, which is not a property to rely on for the markers.  The labels are put
				back in the database collation so filters comparing against them behave as before.  The bound
				only changes a label whose first 160 visible characters sit behind thousands of characters of
				whitespace.
		   */
		   Flat = LTRIM(RTRIM(
					REPLACE(REPLACE(REPLACE(
						REPLACE(REPLACE(REPLACE(
							CONVERT(NVARCHAR(4000), LEFT(T.StatementText, 4000)) COLLATE Latin1_General_100_BIN2,
						CHAR(13), ' '), CHAR(10), ' '), CHAR(9), ' '),
					' ', CHAR(1) + CHAR(2)), CHAR(2) + CHAR(1), ''), CHAR(1) + CHAR(2), ' ')
				))
	FROM (
		SELECT StatementText = CASE WHEN @StatementType = 5 THEN @StatementTemplate
									ELSE (SELECT SUBSTRING(QT.text,
														   ISNULL((NULLIF(@StartOffset, -1) / 2) + 1, 0),
														   ISNULL((NULLIF(NULLIF(@EndOffset, -1), 0) - NULLIF(@StartOffset, -1)) / 2 + 1, 2147483647))
										  FROM dbo.QueryText QT
										  WHERE QT.sql_handle = @sql_handle)
							   END
	) T
) F
