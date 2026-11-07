CREATE PROC Alert.Rule_Add(
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
INSERT INTO Alert.Rules (Type, AlertKey, Priority, ApplyToTagID, ApplyToTagIDs, ApplyToInstanceID, Threshold, EvaluationPeriodMins, IsActive, Details, Notes, ApplyToHidden, GroupID)
SELECT @Type, @AlertKey, @Priority, N.ApplyToTagID, N.ApplyToTagIDs, @ApplyToInstanceID, @Threshold, @EvaluationPeriodMins, @IsActive, @Details, @Notes, @ApplyToHidden, @GroupID
FROM Alert.TagScope_Normalize(@ApplyToTagID, @ApplyToTagIDs) N
