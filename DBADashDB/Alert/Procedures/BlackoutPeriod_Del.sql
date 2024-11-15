CREATE PROC Alert.BlackoutPeriod_Del(
	@BlackoutPeriodID INT
)
AS
DELETE Alert.BlackoutPeriod
WHERE BlackoutPeriodID = @BlackoutPeriodID