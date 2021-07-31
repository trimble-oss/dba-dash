CREATE FUNCTION dbo.MillisecondsToHumanDuration (
    @ms BIGINT
)
RETURNS TABLE
AS
RETURN SELECT CONCAT(
                        RIGHT('00' + CAST(ISNULL((@ms / 3600000 / 24), 0) AS VARCHAR(2)), 2), ' ',
                        RIGHT('00' + CAST(ISNULL(@ms / 3600000 % 24, 0) AS VARCHAR(2)), 2), ':',
                        RIGHT('00' + CAST(ISNULL(@ms / 60000 % 60, 0) AS VARCHAR(2)), 2), ':',
                        RIGHT('00' + CAST(ISNULL(@ms / 1000 % 3600 % 60, 0) AS VARCHAR(2)), 2) + '.',
						RIGHT('000' + CAST(ISNULL(@ms % 1000, 0) AS VARCHAR(3)), 3) 
                    ) AS HumanDuration;