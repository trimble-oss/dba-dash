CREATE PROC Alert.BlackoutPeriod_Add(
	@ApplyToInstanceID INT=-1,
	@ConnectionID NVARCHAR(128) = NULL,
	@ApplyToTagID INT=-1,
	@StartDate DATETIME2,
	@EndDate DATETIME2,
	@AlertKey NVARCHAR(256) = '%',
	@TimeFrom TIME=NULL,
	@TimeTo TIME=NULL,
	@TimeZone NVARCHAR(128) ='UTC',
	@Monday BIT=1,
	@Tuesday BIT=1,
	@Wednesday BIT=1,
	@Thursday BIT=1,
	@Friday BIT=1,
	@Saturday BIT=1,
	@Sunday BIT=1,
	@Notes NVARCHAR(MAX)=NULL,
	@BlackoutPeriodID INT OUT,
	@ApplyToTagIDs VARCHAR(MAX)=NULL /* Comma-separated TagIDs.  Takes precedence over @ApplyToTagID when not empty */
)
AS
IF EXISTS(SELECT 1 FROM Alert.TagScope_Normalize(@ApplyToTagID,@ApplyToTagIDs) N WHERE LEN(N.ApplyToTagIDs) > 800)
BEGIN
	THROW 50000, 'Too many tags selected.  The list of TagIDs is limited to 800 characters.', 1;
END
SET XACT_ABORT ON
DECLARE @NormalizedTagIDs VARCHAR(MAX)

SELECT @ApplyToTagID = N.ApplyToTagID,
	@NormalizedTagIDs = N.ApplyToTagIDs
FROM Alert.TagScope_Normalize(@ApplyToTagID, @ApplyToTagIDs) N

IF @ConnectionID IS NOT NULL AND @ApplyToInstanceID=-1
BEGIN
	SELECT @ApplyToInstanceID = InstanceID 
	FROM dbo.Instances 
	WHERE ConnectionID  = @ConnectionID

	SELECT @ApplyToInstanceID = InstanceID 
	FROM dbo.Instances 
	WHERE InstanceDisplayName = @ConnectionID
	AND @ApplyToInstanceID = -1

	IF @ApplyToInstanceID=-1
	BEGIN
		RAISERROR('Invalid ConnectionID',11,1);
		RETURN;
	END
END

IF @BlackoutPeriodID IS NOT NULL
BEGIN
	UPDATE Alert.BlackoutPeriod
	SET ApplyToInstanceID = @ApplyToInstanceID,
		ApplyToTagID = @ApplyToTagID,
		ApplyToTagIDs = @NormalizedTagIDs,
		StartDate = @StartDate,
		EndDate = @EndDate,
		AlertKey = @AlertKey,
		Monday=@Monday,
		Tuesday=@Tuesday,
		Wednesday=@Wednesday,
		Thursday=@Thursday,
		Friday=@Friday,
		Saturday=@Saturday,
		Sunday=@Sunday,
		TimeZone = @TimeZone,
		TimeFrom = @TimeFrom,
		TimeTo = @TimeTo,
		Notes = @Notes
	WHERE BlackoutPeriodID = @BlackoutPeriodID
END
ELSE
BEGIN
	INSERT INTO Alert.BlackoutPeriod(ApplyToInstanceID,ApplyToTagID,ApplyToTagIDs,StartDate,EndDate,AlertKey,TimeZone,TimeFrom,TimeTo,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday,Notes)
	VALUES(@ApplyToInstanceID,@ApplyToTagID,@NormalizedTagIDs,@StartDate,@EndDate,@AlertKey,@TimeZone,@TimeFrom,@TimeTo,@Monday,@Tuesday,@Wednesday,@Thursday,@Friday,@Saturday,@Sunday,@Notes)

	SET @BlackoutPeriodID = SCOPE_IDENTITY()
END

