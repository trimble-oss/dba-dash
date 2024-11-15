CREATE  FUNCTION Alert.ApplicableInstances_Get(
	@TagID INT, 
	@InstanceID INT,
	@AlertKey NVARCHAR(128)='%'
)
RETURNS TABLE
AS
RETURN
SELECT I.InstanceID, I.scheduler_count
FROM dbo.Instances I
OUTER APPLY Alert.IsBlackoutPeriod(I.InstanceID,@AlertKey) BP
WHERE EXISTS(
			SELECT 1
			WHERE @TagID = -1
			AND @InstanceID IS NULL
			UNION ALL
			SELECT 1 
			WHERE I.InstanceID = @InstanceID
			AND @TagID = -1
			AND @InstanceID IS NOT NULL
			UNION ALL
			SELECT 1 
			FROM dbo.InstanceIDsTags IT
			WHERE IT.InstanceID = I.InstanceID
			AND IT.TagID = @TagID
			AND @InstanceID IS NULL
			UNION ALL
			SELECT 1 
			FROM dbo.InstanceTags IT
			WHERE IT.Instance = I.Instance
			AND IT.TagID = @TagID
			AND @InstanceID IS NULL
	)
AND I.IsActive=1
AND BP.IsBlackout=0

