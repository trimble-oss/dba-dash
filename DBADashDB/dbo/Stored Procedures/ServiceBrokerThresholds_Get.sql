CREATE PROC dbo.ServiceBrokerThresholds_Get(
	@InstanceID INT,
	@DatabaseID INT,
	@schema_name NVARCHAR(128) = N'',
	@queue_name NVARCHAR(128)
)
AS
/*
	Returns a row even if no configuration exists at the specified level - returning the inherited threshold.
	IsInherited returns true if the configuration is inherited.
*/
SELECT TOP(1)
		ISNULL(I.InstanceGroupName,'{Root}') + ISNULL(' \ ' + D.name,'') + ISNULL(' \ ' + NULLIF(CONCAT(NULLIF(P.schema_name,'') + '.', P.queue_name),''),'') AS SelectedObject,
		T.QueueDepthWarningThreshold,
		T.QueueDepthCriticalThreshold,
		T.QueueAgeWarningThreshold,
		T.QueueAgeCriticalThreshold,
		T.TransmissionQueueAgeWarningThreshold,
		T.TransmissionQueueAgeCriticalThreshold,
		T.ConversationCountWarningThreshold,
		T.ConversationCountCriticalThreshold,
		CASE WHEN P.InstanceID = T.InstanceID
					AND P.DatabaseID = T.DatabaseID
					AND P.schema_name = T.schema_name
					AND P.queue_name = T.queue_name
			THEN CAST(0 AS BIT)
			WHEN P.InstanceID = -1 AND P.DatabaseID = -1 AND P.queue_name = '' THEN CAST(0 AS BIT)
			ELSE CAST(1 AS BIT)
			END AS IsInherited,
		CASE WHEN T.queue_name <> '' THEN 'Queue' WHEN T.DatabaseID <> -1 THEN 'Database' WHEN T.InstanceID <> -1 THEN 'Instance' ELSE 'Root' END AS ConfiguredLevel
FROM (SELECT @InstanceID AS InstanceID, @DatabaseID AS DatabaseID, ISNULL(@schema_name,N'') AS schema_name, @queue_name AS queue_name) P
LEFT JOIN dbo.Instances I ON P.InstanceID = I.InstanceID
LEFT JOIN dbo.Databases D ON P.DatabaseID = D.DatabaseID AND D.InstanceID = P.InstanceID
LEFT JOIN dbo.ServiceBrokerThresholds T ON (T.InstanceID = P.InstanceID OR T.InstanceID = -1)
										AND (T.DatabaseID = P.DatabaseID OR T.DatabaseID = -1)
										AND ((T.schema_name = P.schema_name AND T.queue_name = P.queue_name) OR (T.schema_name = '' AND T.queue_name = ''))
ORDER BY T.InstanceID DESC,
		T.DatabaseID DESC,
		T.queue_name DESC
