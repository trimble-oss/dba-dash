CREATE PROCEDURE dbo.CounterThresholds_Upd(
	@InstanceID INT=NULL,
	@object_name NVARCHAR (128),
    @counter_name NVARCHAR (128),
    @instance_name NVARCHAR (128)=NULL,
	@CriticalFrom DECIMAL (28, 9),
    @CriticalTo DECIMAL (28, 9),
	@WarningFrom DECIMAL (28, 9),
	@WarningTo DECIMAL (28, 9),
	@GoodFrom DECIMAL (28, 9),
	@GoodTo DECIMAL (28, 9)
)
AS
IF @InstanceID IS NULL
BEGIN
	UPDATE dbo.Counters
	SET CriticalFrom=@CriticalFrom,
		CriticalTo=@CriticalTo,
		WarningFrom=@WarningFrom,
		WarningTo=@WarningTo,
		GoodFrom=@GoodFrom,
		GoodTo=@GoodTo
	WHERE object_name = @object_name
	AND counter_name = @counter_name
	AND (instance_name = @instance_name OR @instance_name IS NULL)
END
BEGIN
	UPDATE IC
	SET IC.CriticalFrom=@CriticalFrom,
		IC.CriticalTo=@CriticalTo,
		IC.WarningFrom=@WarningFrom,
		IC.WarningTo=@WarningTo,
		IC.GoodFrom=@GoodFrom,
		IC.GoodTo=@GoodTo
	FROM dbo.Counters C
	JOIN dbo.InstanceCounters IC ON C.CounterID = IC.CounterID
	WHERE IC.InstanceID = @InstanceID
	AND C.object_name = @object_name
	AND C.counter_name = @counter_name
	AND (C.instance_name = @instance_name OR @instance_name IS NULL)
END