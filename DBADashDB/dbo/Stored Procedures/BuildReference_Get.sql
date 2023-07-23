CREATE PROC dbo.BuildReference_Get
AS
SELECT BuildReferenceVersion,
       BuildReferenceUpdated,
       Version,
       Name,
       CU,
       SP,
       KBList,
       SupportedUntil,
       DATEDIFF(d,GETUTCDATE(),SupportedUntil) AS DaysUntilSupportExpires,
       MainstreamEndDate,
       DATEDIFF(d,GETUTCDATE(),MainstreamEndDate) AS DaysUntilMainstreamSupportExpires,
       Major,
       Minor,
       Build,
       Revision,
       LatestVersion,
       LatestVersionPatchLevel,
       SPBehind,
       CUBehind,
       IsCurrentBuild,
       LifecycleUrl      
FROM dbo.BuildReference