CREATE PROC dbo.DailyReport_InstanceIDs_Get(
	@ApplyToTagID INT,
	@ApplyToTagIDs VARCHAR(800)=NULL
)
AS
/*	Instances a daily report covers: active instances shown in the summary that match the report's tag scope */
SET NOCOUNT ON
SELECT I.InstanceID
FROM dbo.Instances I
CROSS APPLY Alert.TagScopeMatchesInstance(@ApplyToTagID,@ApplyToTagIDs,I.InstanceID) M
WHERE I.IsActive = 1
AND I.ShowInSummary = 1
AND (@ApplyToTagID = -1 OR M.IsMatch = 1)
