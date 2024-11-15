CREATE PROC Alert.Rules_Get(
    @RuleID INT = NULL
)
AS
SELECT R.RuleID,
       R.Type,
       R.AlertKey,
       R.Priority,
       CONCAT(R.Priority,' - ',
			CASE WHEN R.Priority =0 THEN 'Critical'
				WHEN R.Priority>=1 AND R.Priority<=10 THEN 'High'
				WHEN R.Priority>=11 AND R.Priority<=20 THEN 'Medium'
				WHEN R.Priority>=21 AND R.Priority<=30 THEN 'Low'
				WHEN R.Priority>=31 AND R.Priority<=254 THEN 'Informational'
				ELSE 'Success' END) AS PriorityDescription,
       R.ApplyToTagID,
       R.Threshold,
       R.EvaluationPeriodMins,
       R.IsActive,
       R.Details,
	   CASE WHEN R.ApplyToTagID = -1 THEN '{ALL}' ELSE CONCAT(T.TagName,':',T.TagValue) END AS ApplyToTag,
       R.ApplyToInstanceID,
       I.InstanceDisplayName AS ApplyToInstance,
       R.Notes
FROM Alert.Rules R
LEFT JOIN dbo.Tags T ON R.ApplyToTagID = T.TagID
LEFT JOIN dbo.Instances I ON R.ApplyToInstanceID = I.InstanceID
WHERE (R.RuleID = @RuleID OR @RuleID IS NULL)
OPTION(RECOMPILE)