CREATE PROC ServerExtraProperties_Upd
(
    @InstanceID INT,
    @SnapshotDate DATETIME,
    @ActivePowerPlanGUID UNIQUEIDENTIFIER,
    @ActivePowerPlan VARCHAR(16),
    @ProcessorNameString NVARCHAR(512),
    @SystemManufacturer NVARCHAR(512),
    @SystemProductName NVARCHAR(512),
    @IsAgentRunning BIT,
    @InstantFileInitializationEnabled BIT
)
AS
UPDATE dbo.Instances
SET ActivePowerPlanGUID = @ActivePowerPlanGUID,
    ActivePowerPlan = @ActivePowerPlan,
    ProcessorNameString = @ProcessorNameString,
    SystemManufacturer = @SystemManufacturer,
    SystemProductName = @SystemProductName,
    IsAgentRunning = @IsAgentRunning,
    InstantFileInitializationEnabled = @InstantFileInitializationEnabled
WHERE InstanceID = @InstanceID;