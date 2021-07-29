CREATE FUNCTION dbo.SecondsToHumanDuration (
    @Seconds INT
)
RETURNS TABLE
AS
RETURN SELECT CONCAT(
                        RIGHT('00' + CAST(ISNULL((@Seconds / 3600 / 24), 0) AS VARCHAR(2)), 2), ' ',
                        RIGHT('00' + CAST(ISNULL(@Seconds / 3600 % 24, 0) AS VARCHAR(2)), 2), ':',
                        RIGHT('00' + CAST(ISNULL(@Seconds / 60 % 60, 0) AS VARCHAR(2)), 2), ':',
                        RIGHT('00' + CAST(ISNULL(@Seconds % 3600 % 60, 0) AS VARCHAR(2)), 2)
                    ) AS [dd hh:mm:ss];