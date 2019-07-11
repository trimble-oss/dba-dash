CREATE PROC SysConfig_Upd(@Config SysConfig READONLY,@InstanceID INT,@SnapshotDate DATETIME)
AS
DELETE SysConfig
WHERE InstanceID=@InstanceID

INSERT INTO [dbo].[SysConfig]
           ([InstanceID]
           ,[configuration_id]
           ,[value]
           ,[value_in_use])
SELECT @InstanceID InstanceID,
           [configuration_id],
           [value],
           [value_in_use]
FROM @Config