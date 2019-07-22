CREATE PROC [dbo].[SysConfig_Upd](@SysConfig SysConfig READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
INSERT INTO dbo.SysConfig
(
    InstanceID,
    configuration_id,
    value,
    value_in_use,
	ValidFrom
)
SELECT @InstanceID,
           configuration_id,
           value,
           value_in_use,
		   @SnapshotDate ValidFrom 
FROM @SysConfig T
WHERE NOT EXISTS(SELECT 1 FROM dbo.SysConfig SC WHERE T.configuration_id = SC.configuration_id AND SC.InstanceID = @InstanceID);

WITH T AS(
	SELECT InstanceID,
           configuration_id,
           value,
           value_in_use,
		   ValidFrom 
	FROM dbo.SysConfig
	WHERE InstanceID = @InstanceID
)
MERGE T 
USING (SELECT configuration_id,
              value,
              value_in_use 
		FROM @SysConfig) S 
			ON S.configuration_id = T.configuration_id
WHEN MATCHED AND NOT (
				(S.value=T.value OR (S.value IS NULL AND T.value IS NULL))
				AND (S.value_in_use = T.value_in_use OR (S.value_in_use IS NULL AND T.value_in_use IS NULL))
				)
THEN 
UPDATE SET T.value = S.value ,
	T.value_in_use = S.value_in_use,
	T.ValidFrom=@SnapshotDate
WHEN NOT MATCHED BY SOURCE
THEN DELETE
OUTPUT DELETED.InstanceID,
		DELETED.configuration_id,
		DELETED.value,
		INSERTED.value AS new_value,
		DELETED.value_in_use,
		INSERTED.value_in_use AS new_value_in_use,
		DELETED.ValidFrom,
		@SnapshotDate 
INTO dbo.SysConfigHistory(InstanceID,configuration_id,value,new_value,value_in_use,new_value_in_use,ValidFrom,ValidTo);