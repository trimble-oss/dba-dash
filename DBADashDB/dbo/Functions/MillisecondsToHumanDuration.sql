CREATE FUNCTION dbo.MillisecondsToHumanDuration (
    @ms BIGINT
)
RETURNS TABLE
AS
RETURN SELECT CASE WHEN ABS(@ms)>=8640000000 THEN CONCAT(@ms/86400000,' Days') ELSE
				CONCAT(
						CASE WHEN @ms<0 THEN '-' ELSE '' END,
                        RIGHT('00' + CAST(ISNULL((ABS(@ms) / 3600000 / 24), 0) AS VARCHAR(2)), 2), ' ',
                        RIGHT('00' + CAST(ISNULL(ABS(@ms) / 3600000 % 24, 0) AS VARCHAR(2)), 2), ':',
                        RIGHT('00' + CAST(ISNULL(ABS(@ms) / 60000 % 60, 0) AS VARCHAR(2)), 2), ':',
                        RIGHT('00' + CAST(ISNULL(ABS(@ms) / 1000 % 3600 % 60, 0) AS VARCHAR(2)), 2) + '.',
						RIGHT('000' + CAST(ISNULL(ABS(@ms) % 1000, 0) AS VARCHAR(3)), 3) 
                    )
				END AS HumanDuration;