CREATE PROC Alert.Rule_Upd(
	@RuleID INT,
	@Type VARCHAR(50),
	@AlertKey NVARCHAR(128),
	@Priority INT,
	@ApplyToTagID INT=-1,
	@ApplyToInstanceID INT=NULL,
	@Threshold DECIMAL(28,9),
	@EvaluationPeriodMins INT,
	@IsActive BIT=1,
	@Details NVARCHAR(MAX),
	@Notes NVARCHAR(MAX)=NULL
)
AS
UPDATE Alert.Rules SET
	Type = @Type,
	AlertKey = @AlertKey,
	Priority = @Priority,
	ApplyToTagID = @ApplyToTagID,
	ApplyToInstanceID=@ApplyToInstanceID,
	Threshold = @Threshold,
	EvaluationPeriodMins = @EvaluationPeriodMins,
	IsActive = @IsActive,
	Details = @Details,
	Notes = @Notes
WHERE RuleID = @RuleID