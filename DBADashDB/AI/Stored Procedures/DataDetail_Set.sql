CREATE PROCEDURE AI.DataDetail_Set
    @DataDetail NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    -- What the built-in AI sends to its provider is an administrator's decision, so only db_owner may change it.
    IF IS_MEMBER('db_owner') <> 1
    BEGIN
        THROW 50000, 'Only a db_owner can change the data detail the built-in AI sends to its provider.', 1;
    END

    IF @DataDetail IS NULL OR @DataDetail NOT IN (N'full', N'parameters', N'literals', N'parametersAndLiterals', N'metadataOnly')
    BEGIN
        THROW 50001, 'DataDetail must be full, parameters, literals, parametersAndLiterals or metadataOnly.', 1;
    END

    MERGE AI.DataDetail AS target
    USING (VALUES (1, @DataDetail)) AS source (ConfigId, DataDetail)
        ON target.ConfigId = source.ConfigId
    WHEN MATCHED THEN
        UPDATE SET DataDetail = source.DataDetail,
                   LastModifiedDate = SYSDATETIME(),
                   LastModifiedBy = SUSER_SNAME()
    WHEN NOT MATCHED THEN
        INSERT (ConfigId, DataDetail) VALUES (source.ConfigId, source.DataDetail);
END
GO
