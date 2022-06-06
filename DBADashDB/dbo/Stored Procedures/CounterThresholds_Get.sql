CREATE PROCEDURE dbo.CounterThresholds_Get(
	@InstanceID INT=NULL,
	@object_name NVARCHAR (128),
    @counter_name NVARCHAR (128),
    @instance_name NVARCHAR (128)
)
AS
IF @InstanceID IS NULL
BEGIN
	SELECT C.CriticalFrom,
		C.CriticalTo,
		C.WarningFrom,
		C.WarningTo,
		C.GoodFrom,
		C.GoodTo
	FROM dbo.Counters C
	WHERE C.object_name = @object_name
	AND C.counter_name = @counter_name
	AND C.instance_name = @instance_name
END
BEGIN
	SELECT IC.CriticalFrom,
		IC.CriticalTo,
		IC.WarningFrom,
		IC.WarningTo,
		IC.GoodFrom,
		IC.GoodTo
	FROM dbo.Counters C
	JOIN dbo.InstanceCounters IC ON IC.CounterID = C.CounterID
	WHERE C.object_name = @object_name
	AND C.counter_name = @counter_name
	AND C.instance_name = @instance_name
	AND IC.InstanceID = @InstanceID
END