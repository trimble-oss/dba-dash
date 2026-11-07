CREATE FUNCTION Alert.IsBlackoutPeriod(
	@InstanceID INT=-1,
	@AlertKey NVARCHAR(256)='%'
)
RETURNS TABLE
AS
/*
	Is an alert for the instance muted right now?  Used where alerts are checked outside the per alert run tables (active alerts, SQL Agent alert import).
	Alert.ApplicableInstances_Upd resolves the same for every instance into Alert.ActiveBlackout.
*/
RETURN
SELECT CASE WHEN I.CollectionsPausedUntil > SYSUTCDATETIME() THEN CAST(1 AS BIT) /* Collections paused from the GUI - alerts are muted until they resume */
			WHEN EXISTS(
				SELECT 1
				FROM Alert.BlackoutPeriodsInEffect_Get() BP
				WHERE @AlertKey LIKE BP.AlertKey
				AND EXISTS(
							/* Apply to all tags/instances */
							SELECT 1
							WHERE BP.ApplyToTagID = -1
							AND BP.ApplyToInstanceID = -1
							UNION ALL
							/* Apply to specific instance */
							SELECT 1
							WHERE BP.ApplyToInstanceID = @InstanceID
							AND BP.ApplyToTagID = -1
							UNION ALL
							/* Apply to tag */
							SELECT 1
							FROM dbo.InstanceIDsTags IT
							WHERE IT.InstanceID = @InstanceID
							AND IT.TagID = BP.ApplyToTagID
							AND BP.ApplyToInstanceID = -1
							UNION ALL
							/* Apply to tag (Azure instance) */
							SELECT 1
							FROM dbo.InstanceTags IT
							WHERE IT.Instance = I.Instance
							AND IT.TagID = BP.ApplyToTagID
							AND BP.ApplyToInstanceID = -1
							UNION ALL
							/* Apply to multiple tags */
							SELECT 1
							FROM Alert.TagScopeMatchesInstance(BP.ApplyToTagID,BP.ApplyToTagIDs,@InstanceID) TM
							WHERE BP.ApplyToTagID = -2
							AND BP.ApplyToInstanceID = -1
							AND TM.IsMatch = 1
					)
			) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsBlackout
FROM dbo.Instances I
WHERE InstanceID = @InstanceID
