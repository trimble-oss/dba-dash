CREATE PROC dbo.Alerts_Ack(
	@InstanceID INT=NULL,
	@id INT=NULL,
	@Clear BIT=0
)
AS
UPDATE A
	SET A.AcknowledgeDate = CASE WHEN @Clear = 1 THEN NULL ELSE GETUTCDATE() END
FROM dbo.Alerts A
WHERE (A.InstanceID = @InstanceID OR @InstanceID IS NULL)
AND (A.id = @id OR @id IS NULL)
/* Only set AcknowledgeDate where required for Warning/Critical status */
AND (EXISTS(SELECT 1 
			FROM dbo.SysAlerts SA
			WHERE SA.InstanceID = A.InstanceID
			AND SA.id = A.id 
			AND SA.AlertStatus IN(1,2) 
			)
		OR (@Clear=1 AND A.AcknowledgeDate IS NOT NULL)
	)
OPTION(RECOMPILE)