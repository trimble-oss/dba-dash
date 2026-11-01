CREATE PROCEDURE AI.DataDetail_Get
AS
BEGIN
    SET NOCOUNT ON;

    -- parametersAndLiterals when no row has been saved, so a repository that has never had the setting changed is masked.
    SELECT COALESCE(
        (SELECT DataDetail FROM AI.DataDetail WHERE ConfigId = 1),
        N'parametersAndLiterals') AS DataDetail;
END
GO
