CREATE PROC dbo.BusyPeriod_Upd(
	@BusyPeriodID INT=NULL OUT,
	@ApplyToInstanceID INT=-1,
	@ApplyToTagID INT=-1,
	@Monday BIT=1,
	@Tuesday BIT=1,
	@Wednesday BIT=1,
	@Thursday BIT=1,
	@Friday BIT=1,
	@Saturday BIT=1,
	@Sunday BIT=1,
	@TimeFrom TIME,
	@TimeTo TIME,
	@TimeZone NVARCHAR(128)='UTC',
	@Description NVARCHAR(256)=NULL
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
/* Adds a busy period when @BusyPeriodID is NULL, otherwise updates that period. The new ID is returned in @BusyPeriodID. */
IF @BusyPeriodID IS NOT NULL
BEGIN
	UPDATE dbo.BusyPeriod
	SET ApplyToInstanceID = @ApplyToInstanceID,
		ApplyToTagID = @ApplyToTagID,
		Monday = @Monday,
		Tuesday = @Tuesday,
		Wednesday = @Wednesday,
		Thursday = @Thursday,
		Friday = @Friday,
		Saturday = @Saturday,
		Sunday = @Sunday,
		TimeFrom = @TimeFrom,
		TimeTo = @TimeTo,
		TimeZone = @TimeZone,
		Description = @Description
	WHERE BusyPeriodID = @BusyPeriodID
END
ELSE
BEGIN
	INSERT INTO dbo.BusyPeriod(ApplyToInstanceID, ApplyToTagID, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday, TimeFrom, TimeTo, TimeZone, Description)
	VALUES(@ApplyToInstanceID, @ApplyToTagID, @Monday, @Tuesday, @Wednesday, @Thursday, @Friday, @Saturday, @Sunday, @TimeFrom, @TimeTo, @TimeZone, @Description)

	SET @BusyPeriodID = SCOPE_IDENTITY()
END
