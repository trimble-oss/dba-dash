/*
    The whole batch or module definition a stored statement came from.

    Exists so that the query stats grid does not have to carry it.  For a statement inside a procedure the
    batch is the procedure's definition, and returning one on every row of a grid of up to a thousand rows is a
    lot to send for the one a reader opens.  The grid carries the statement's id instead and this fetches the
    one batch on demand: a seek on the statement, then one on its text.

    For an ad hoc shape the batch is its example's - one variant among however many the shape ran with - and it
    says so at the top, in the text itself, so the note travels with it if it is copied out and run.  Its values
    are one execution's, and the shape's statistics are every variant's together.

    Returns no rows when the text has not been collected yet - it is fetched separately from the statistics
    and capped per collection - or has been purged.  The caller reports that as "no text available" rather
    than as an error, because both are ordinary states.
*/
CREATE PROC dbo.QueryStatementBatchText_Get (
    @StatementID BIGINT
)
AS
SET NOCOUNT ON

SELECT BatchText = CASE WHEN S.StatementType = 5
                        THEN N'/*
    An example: one of the texts this ad hoc query shape ran with.  Its literal values are this one
    variant''s, while the statistics cover every variant of the shape together, and other values can cost far
    more or far less than these.  Cache Entries in the grid says roughly how many texts there were.
*/
' + QT.text
                        ELSE QT.text
                   END,
       IsExample = CONVERT(BIT, CASE WHEN S.StatementType = 5 THEN 1 ELSE 0 END)
FROM dbo.QueryStatements S
JOIN dbo.QueryText QT ON QT.sql_handle = S.sql_handle
WHERE S.StatementID = @StatementID;
