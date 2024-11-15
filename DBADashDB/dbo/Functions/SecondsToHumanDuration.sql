CREATE FUNCTION dbo.SecondsToHumanDuration (
    @Seconds BIGINT
)
RETURNS TABLE
AS
RETURN SELECT CASE WHEN @Seconds IS NULL THEN '' WHEN ABS(@Seconds)>=8640000 THEN CONCAT(@Seconds/86400,' Days') ELSE
				CONCAT(
						CASE WHEN @Seconds<0 THEN '-' ELSE '' END,
                        RIGHT('00' + CAST(ISNULL((ABS(@Seconds) / 3600 / 24), 0) AS VARCHAR(2)), 2), ' ',
                        RIGHT('00' + CAST(ISNULL(ABS(@Seconds) / 3600 % 24, 0) AS VARCHAR(2)), 2), ':',
                        RIGHT('00' + CAST(ISNULL(ABS(@Seconds) / 60 % 60, 0) AS VARCHAR(2)), 2), ':',
                        RIGHT('00' + CAST(ISNULL(ABS(@Seconds) % 3600 % 60, 0) AS VARCHAR(2)), 2)
                    )
				END AS HumanDuration;