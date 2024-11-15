CREATE PROC Alert.Rule_Add(
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
INSERT INTO Alert.Rules (Type, AlertKey, Priority, ApplyToTagID,ApplyToInstanceID, Threshold, EvaluationPeriodMins, IsActive, Details, Notes)
VALUES (@Type, @AlertKey, @Priority, @ApplyToTagID,@ApplyToInstanceID, @Threshold, @EvaluationPeriodMins, @IsActive, @Details, @Notes)