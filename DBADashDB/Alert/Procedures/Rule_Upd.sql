CREATE PROC Alert.Rule_Upd(
	@RuleID INT,
	@Type VARCHAR(50),
	@AlertKey NVARCHAR(256),
	@Priority INT,
	@ApplyToTagID INT=-1,
	@ApplyToInstanceID INT=NULL,
	@Threshold DECIMAL(28,9),
	@EvaluationPeriodMins INT,
	@IsActive BIT=1,
	@Details NVARCHAR(MAX),
	@Notes NVARCHAR(MAX)=NULL,
	@ApplyToHidden BIT=0,
	@GroupID INT=0,
	@ApplyToTagIDs VARCHAR(MAX)=NULL /* Comma-separated TagIDs.  Takes precedence over @ApplyToTagID when not empty */
)
AS
IF EXISTS(SELECT 1 FROM Alert.TagScope_Normalize(@ApplyToTagID,@ApplyToTagIDs) N WHERE LEN(N.ApplyToTagIDs) > 800)
BEGIN
	THROW 50000, 'Too many tags selected.  The list of TagIDs is limited to 800 characters.', 1;
END
UPDATE R SET
	Type = @Type,
	AlertKey = @AlertKey,
	Priority = @Priority,
	ApplyToTagID = N.ApplyToTagID,
	ApplyToTagIDs = N.ApplyToTagIDs,
	ApplyToInstanceID=@ApplyToInstanceID,
	Threshold = @Threshold,
	EvaluationPeriodMins = @EvaluationPeriodMins,
	IsActive = @IsActive,
	Details = @Details,
	Notes = @Notes,
	ApplyToHidden = @ApplyToHidden,
	GroupID = @GroupID
FROM Alert.Rules R
CROSS APPLY Alert.TagScope_Normalize(@ApplyToTagID, @ApplyToTagIDs) N
WHERE R.RuleID = @RuleID
