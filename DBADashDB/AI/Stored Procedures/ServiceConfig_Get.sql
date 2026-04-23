CREATE PROCEDURE AI.ServiceConfig_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ConfigId,
        ServiceUrl,
        ApiKey,
        PreviousApiKey,
        PreviousApiKeyExpiryUtc,
        IsEnabled,
        LastHeartbeat,
        ServiceVersion,
        ApiKeyCreatedDate,
        ApiKeyCreatedBy,
        CreatedDate,
        LastModifiedDate,
        -- Helper: Is service recently active (heartbeat within 2 minutes)
        CASE 
            WHEN LastHeartbeat IS NULL THEN 0
            WHEN DATEDIFF(SECOND, LastHeartbeat, SYSDATETIME()) <= 120 THEN 1
            ELSE 0
        END AS IsActive,
        -- Whether the calling user can see the real ApiKey value.
        -- SQL Server 2022+ may use a granular column-level/object-level UNMASK grant,
        -- while older supported versions only support database-level UNMASK.
        -- The GUI uses this flag to decide whether to show the AI Assistant tab;
        -- users without access see the masked placeholder 'xxxx'.
        CAST(CASE 
            WHEN HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'UNMASK') = 1 THEN 1
            WHEN HAS_PERMS_BY_NAME('AI.ServiceConfig', 'OBJECT', 'UNMASK', 'ApiKey', 'COLUMN') = 1 THEN 1
            ELSE 0
        END AS BIT) AS HasApiKeyAccess
    FROM AI.ServiceConfig
    WHERE ConfigId = 1;
END
GO
