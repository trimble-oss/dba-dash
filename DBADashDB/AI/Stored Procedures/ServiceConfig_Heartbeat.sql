CREATE PROCEDURE AI.ServiceConfig_Heartbeat
    @ServiceVersion NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Update heartbeat timestamp
    UPDATE AI.ServiceConfig
    SET LastHeartbeat = SYSDATETIME(),
        ServiceVersion = COALESCE(@ServiceVersion, ServiceVersion),
        LastModifiedDate = SYSDATETIME()
    WHERE ConfigId = 1;

    -- If no rows updated, config doesn't exist yet
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('AIServiceConfig not initialized. Service must register first.', 16, 1);
    END
END
GO
