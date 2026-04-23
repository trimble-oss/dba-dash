CREATE PROCEDURE AI.ServiceConfig_Upd
    @ServiceUrl NVARCHAR(500),
    @ApiKey NVARCHAR(256),
    @IsEnabled BIT = 1,
    @ServiceVersion NVARCHAR(50) = NULL,
    @KeyGracePeriodHours INT = 24
AS
BEGIN
    SET NOCOUNT ON;

    -- Upsert pattern: update if exists, insert if not
    IF EXISTS (
                SELECT ServiceUrl, ApiKey, IsEnabled, ServiceVersion
                FROM AI.ServiceConfig
                WHERE ConfigId = 1
                EXCEPT
                SELECT @ServiceUrl, @ApiKey, @IsEnabled, @ServiceVersion
          )
    BEGIN
        UPDATE AI.ServiceConfig
        SET ServiceUrl        = @ServiceUrl,
            -- When key changes, preserve the old key with a grace period expiry
            PreviousApiKey         = CASE WHEN @ApiKey <> ApiKey THEN ApiKey ELSE PreviousApiKey END,
            PreviousApiKeyExpiryUtc = CASE WHEN @ApiKey <> ApiKey THEN DATEADD(HOUR, @KeyGracePeriodHours, SYSUTCDATETIME()) ELSE PreviousApiKeyExpiryUtc END,
            ApiKey            = @ApiKey,
            IsEnabled         = @IsEnabled,
            ServiceVersion    = @ServiceVersion,
            ApiKeyCreatedDate = CASE WHEN @ApiKey <> ApiKey THEN SYSDATETIME() ELSE ApiKeyCreatedDate END,
            ApiKeyCreatedBy   = CASE WHEN @ApiKey <> ApiKey THEN SUSER_SNAME()  ELSE ApiKeyCreatedBy   END,
            LastModifiedDate  = SYSUTCDATETIME()
        WHERE ConfigId = 1
    END
    IF NOT EXISTS(SELECT 1 FROM AI.ServiceConfig WHERE ConfigId = 1)
    BEGIN
        INSERT INTO AI.ServiceConfig (
            ConfigId,
            ServiceUrl,
            ApiKey,
            IsEnabled,
            ServiceVersion,
            ApiKeyCreatedDate,
            ApiKeyCreatedBy,
            CreatedDate,
            LastModifiedDate
        )
        VALUES (
            1,
            @ServiceUrl,
            @ApiKey,
            @IsEnabled,
            @ServiceVersion,
            SYSDATETIME(),
            SUSER_SNAME(),
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        );
    END

    -- Return the config
    EXEC AI.ServiceConfig_Get;
END
GO
