SET NOCOUNT ON
DECLARE @ProcessorNameString NVARCHAR(512)
DECLARE @SystemManufacturer NVARCHAR(512)
DECLARE @SystemProductName NVARCHAR(512)
DECLARE @IsAgentRunning BIT
DECLARE @InstantFileInitializationEnabled BIT
IF OBJECT_ID('sys.xp_instance_regread') IS NOT NULL AND IS_SRVROLEMEMBER('sysadmin')=1
BEGIN  
	EXEC sys.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'HARDWARE\DESCRIPTION\System\CentralProcessor\0', N'ProcessorNameString',@ProcessorNameString OUT;
	EXEC sys.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'SYSTEM\HardwareConfig\Current', N'SystemManufacturer',@SystemManufacturer OUT;
	EXEC sys.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'SYSTEM\HardwareConfig\Current', N'SystemProductName', @SystemProductName OUT;
END
ELSE
BEGIN
	PRINT 'Sysadmin required to get ProcessorNameString, SystemManufacturer and SystemProductName'
END
DECLARE @ActivePowerPlan UNIQUEIDENTIFIER
DECLARE @HighPerformance VARCHAR(36)  
DECLARE @Balanced  VARCHAR(36) 
DECLARE @PowerSaver  VARCHAR(36)
SELECT @HighPerformance='8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c',@Balanced='381b4222-f694-41f0-9685-ff5bb260df2e',@PowerSaver='a1841308-3541-4fab-bc81-f71556f20b4a'
DECLARE @output TABLE(
	output NVARCHAR(MAX)
)
IF EXISTS(SELECT * FROM sys.configurations
WHERE name = 'xp_cmdshell'
AND value_in_use=1
) AND EXISTS(SELECT * FROM fn_my_permissions ( 'xp_cmdshell', 'OBJECT' ) WHERE permission_name='EXECUTE')
BEGIN
	BEGIN TRY
	INSERT INTO @output
	(
		output
	)
	EXEC xp_cmdshell 'powercfg /list';
	SELECT @ActivePowerPlan = CAST(MAX(CASE WHEN output LIKE  '%' + @HighPerformance + '%*%' THEN @HighPerformance WHEN output LIKE '%' + @Balanced + '%*%' THEN @Balanced WHEN output LIKE '%' + @PowerSaver + '%*%' THEN @PowerSaver ELSE NULL END) AS UNIQUEIDENTIFIER)
	FROM @output
	END TRY
	BEGIN CATCH
		PRINT 'Unable to get powerplan using xp_cmdshell: ' + ERROR_MESSAGE()
	END CATCH

END
ELSE
BEGIN
	PRINT 'Enable xp_cmdshell to check power plan'
END

IF COL_LENGTH('sys.dm_server_services','instant_file_initialization_enabled') IS NOT NULL
BEGIN
	SELECT @InstantFileInitializationEnabled=CASE WHEN instant_file_initialization_enabled='Y' THEN 1 ELSE 0 END
	FROM   sys.dm_server_services dss
	WHERE  dss.[servicename] LIKE N'SQL Server (%';
END

SELECT @IsAgentRunning = CASE WHEN EXISTS(SELECT * 
											FROM sys.dm_exec_sessions
											WHERE program_name = 'SQLAgent - Generic Refresher'
										)
							THEN CAST(1 AS BIT)
							ELSE CAST(0 AS BIT)
							END

DECLARE @OfflineSchedulers INT
DECLARE @ResourceGovernorEnabled BIT
SELECT @OfflineSchedulers=COUNT(*)
FROM sys.dm_os_schedulers
WHERE is_online=0
IF OBJECT_ID('sys.resource_governor_configuration') IS  NOT NULL
BEGIN
	SELECT  @ResourceGovernorEnabled = CASE WHEN EXISTS(SELECT * 
										FROM sys.resource_governor_configuration
										WHERE is_enabled=1)
										THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
END
DECLARE @LastMemoryDump DATETIME
DECLARE @DumpCount INT
IF OBJECT_ID('sys.dm_server_memory_dumps') IS NOT NULL
BEGIN
	select @LastMemoryDump=MAX(creation_time),@DumpCount=COUNT(*) 
	from sys.dm_server_memory_dumps
END

DECLARE @DBMailStatus NVARCHAR(500)
DECLARE @SysMailHelpStatus TABLE(
	Status NVARCHAR(7) NULL
)
BEGIN TRY
	INSERT INTO @SysMailHelpStatus
	(
		Status
	)
	EXEC msdb.dbo.sysmail_help_status_sp

	SELECT @DBMailStatus = Status  
	FROM @SysMailHelpStatus
END TRY
BEGIN CATCH
	SET @DBMailStatus =CAST(ERROR_NUMBER() AS NVARCHAR(MAX)) + '|' + ERROR_MESSAGE()
END CATCH

SELECT @ActivePowerPlan ActivePowerPlanGUID,
       CASE
           WHEN @ActivePowerPlan = @HighPerformance THEN
               'High Performance'
           WHEN @ActivePowerPlan = @Balanced THEN
               'Balanced'
           WHEN @ActivePowerPlan = @PowerSaver THEN
               'Power Saver'
           ELSE
               NULL
       END AS ActivePowerPlan,
       @ProcessorNameString AS ProcessorNameString,
       @SystemManufacturer AS SystemManufacturer,
       @SystemProductName AS SystemProductName,
       @IsAgentRunning IsAgentRunning,
       @InstantFileInitializationEnabled InstantFileInitializationEnabled,
       @OfflineSchedulers OfflineSchedulers,
       @ResourceGovernorEnabled AS ResourceGovernorEnabled,
       @LastMemoryDump LastMemoryDump,
       @DumpCount AS MemoryDumpCount,
	   @DBMailStatus AS DBMailStatus;