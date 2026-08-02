CREATE PROC dbo.AgentJobExclusions_Del(
    @AgentJobExclusionID INT
)
AS
SET XACT_ABORT ON
SET NOCOUNT ON

DELETE dbo.AgentJobExclusions
WHERE AgentJobExclusionID = @AgentJobExclusionID
