CREATE PROC dbo.BuildReference_Upd(
	@BuildReferenceJson NVARCHAR(MAX)
)
AS
SET XACT_ABORT ON
CREATE TABLE #Parsed(
	BuildReferenceVersion DATETIME NOT NULL,
	Version NVARCHAR(128) NOT NULL,
	Name NVARCHAR(128) NULL,
	CU NVARCHAR(128) NULL,
	SP NVARCHAR(128) NULL,
	KBList NVARCHAR(MAX) NULL,
	SupportedUntil DATETIME NULL,	
	Major INT NOT NULL,
	Minor INT NOT NULL,
	Build INT NOT NULL,
	Revision INT NULL,
	LatestVersion NVARCHAR(128),
	LatestVersionPatchLevel NVARCHAR(128),
	SPBehind INT NULL,
	CUBehind INT NULL,
	IsCurrentBuild BIT NULL,
	LifecycleUrl NVARCHAR(MAX),
	MainstreamEndDate DATETIME NULL,
	PRIMARY KEY(Version),
	UNIQUE (Major,Minor,Build,Revision)
)
DECLARE @CurrentBuildReferenceVersion DATETIME
SELECT TOP(1) @CurrentBuildReferenceVersion = BuildReferenceVersion
FROM dbo.BuildReference

INSERT INTO #Parsed(
		BuildReferenceVersion,
		Version,
		Name,
		CU,
		SP,
		KBList,
		SupportedUntil,	
		Major,
		Minor,
		Build,
		Revision
)
SELECT	LastUpdated,
		Version,
		Name,
		CU,
		SP,
		ISNULL(KBList,STUFF((SELECT ',' + value FROM OPENJSON(KBListJson) KB FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')) AS KBList,
		SupportedUntil,	
		CAST(PARSENAME(Version,C.VersionParts) AS INT) as Major,
		CAST(PARSENAME(Version,C.VersionParts-1) AS INT) as Minor,
		CAST(PARSENAME(Version,C.VersionParts-2) AS INT) as Build,
		CAST(PARSENAME(Version,C.VersionParts-3) AS INT) as Revision
FROM OPENJSON(@BuildReferenceJson) WITH(
	LastUpdated DATETIME '$.LastUpdated',
	Data NVARCHAR(MAX) '$.Data' AS JSON
	)
OUTER APPLY OPENJSON(Data) WITH(
	Version NVARCHAR(128) '$.Version',
	Name NVARCHAR(128) '$.Name',
	CU NVARCHAR(128) '$.CU',
	SP NVARCHAR(128) '$.SP',
	KBList NVARCHAR(MAX) '$.KBList',
	KBListJson NVARCHAR(MAX) '$.KBList' AS JSON,
	SupportedUntil DATETIME '$.SupportedUntil'
	)
OUTER APPLY(
			SELECT COUNT(*) AS VersionParts 
			FROM STRING_SPLIT(Version,'.')
			) C
WHERE Version NOT LIKE '8.%' /* Exclude 2000 */;

/* Check if the version we are updating is older than the current version */
IF EXISTS(	SELECT * 
			FROM #Parsed
			WHERE BuildReferenceVersion < @CurrentBuildReferenceVersion)
BEGIN
	RAISERROR('Version is older than current version',11,1);
	RETURN;
END;

WITH A AS(
	SELECT	*,
			FIRST_VALUE(Version) OVER(PARTITION BY Major,Minor ORDER BY Build DESC, Revision DESC) AS CalcLatestVersion
	FROM #Parsed
)
UPDATE A  
		SET A.Name = ISNULL(A.Name,LastName.Name),
		A.SP = COALESCE(A.SP,LastSP.SP,''),
		A.SupportedUntil = ISNULL(A.SupportedUntil,LastSU.SupportedUntil),
		A.LatestVersion = A.CalcLatestVersion,
		A.IsCurrentBuild = CASE WHEN A.Version = A.CalcLatestVersion THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
FROM A
/* Name is populated for first entry only.  Get the first name that matches this version Major.Minor, older than this version. */
OUTER APPLY(
		SELECT TOP(1) B.Name
		FROM #Parsed B
		WHERE A.Major = B.Major
		AND A.Minor= B.Minor
		AND (A.Build > B.Build OR (A.Build = B.Build AND A.Revision > B.Revision))
		AND B.Name IS NOT NULL
		ORDER BY B.Build DESC, B.Revision DESC
		) LastName
/* Find the most recent NOT NULL SP for this Major.Minor version older than this version */
OUTER APPLY(
		SELECT TOP(1) B.SP
		FROM #Parsed B
		WHERE A.Major = B.Major
		AND A.Minor= B.Minor
		AND (A.Build > B.Build OR (A.Build = B.Build AND A.Revision > B.Revision))
		AND B.SP IS NOT NULL
		ORDER BY B.Build DESC, B.Revision DESC
		) LastSP
/* Find the most recent NOT NULL SupportedUntil for this version Major.Minor, older than this version */
OUTER APPLY(
		SELECT TOP(1) B.SupportedUntil
		FROM #Parsed B
		WHERE A.Major = B.Major
		AND A.Minor= B.Minor
		AND (A.Build > B.Build OR (A.Build = B.Build AND A.Revision > B.Revision))
		AND B.SupportedUntil IS NOT NULL
		ORDER BY B.Build DESC, B.Revision DESC
		) LastSU

UPDATE A
		SET A.CU = COALESCE(A.CU,LastCU.CU,'')
FROM #Parsed A
/* Find the most recent NOT NULL CU for this Major.Minor version and SP, older than this version */
OUTER APPLY(
		SELECT TOP(1) B.CU
		FROM #Parsed B
		WHERE A.Major = B.Major
		AND A.Minor= B.Minor
		AND (A.Build > B.Build OR (A.Build = B.Build AND A.Revision > B.Revision))
		AND B.CU IS NOT NULL
		AND A.SP = B.SP
		ORDER BY B.Build DESC, B.Revision DESC
		) LastCU


UPDATE A	
		SET A.SPBehind = SP.SPBehind,
		A.CUBehind = CU.CUBehind,
		A.LatestVersionPatchLevel = CONCAT(B.SP + ' ', B.CU)
FROM #Parsed A
LEFT JOIN #Parsed B ON A.LatestVersion = B.Version
/* Count how many SPs we are behind the most recent SP */
OUTER APPLY(SELECT COUNT(DISTINCT SP)-1 SPBehind
			FROM #Parsed B
			WHERE A.Major = B.Major
			AND A.Minor = B.Minor
			AND A.Build <= B.Build
			) AS SP
/* Count how many CUs we are behind the most recent CU for this SP */
OUTER APPLY(SELECT COUNT(DISTINCT CU)-1 CUBehind
			FROM #Parsed B
			WHERE A.Major = B.Major
			AND A.Minor = B.Minor
			AND A.Build <= B.Build
			AND A.SP = B.SP
			) AS CU;

/* 
	Populate lifecycle links and mainstream support end dates.
	Hard coded here as they are not provided by dbatools build reference and don't change frequently.
*/
WITH T AS (SELECT * 
FROM  (VALUES
		('2005','https://learn.microsoft.com/en-us/lifecycle/products/microsoft-sql-server-2005','20110412'),
		('2008','https://learn.microsoft.com/en-us/lifecycle/products/microsoft-sql-server-2008','20140708'),
		('2008R2','https://learn.microsoft.com/en-us/lifecycle/products/microsoft-sql-server-2008-r2','20140708'),
		('2012','https://learn.microsoft.com/en-us/lifecycle/products/microsoft-sql-server-2012','20170711'),
		('2014','https://learn.microsoft.com/en-us/lifecycle/products/sql-server-2014','20190709'),
		('2016','https://learn.microsoft.com/en-us/lifecycle/products/sql-server-2016','20210713'),
		('2017','https://learn.microsoft.com/en-us/lifecycle/products/sql-server-2017','20221011'),
		('2019','https://learn.microsoft.com/en-us/lifecycle/products/sql-server-2019','20250228'),
		('2022','https://learn.microsoft.com/en-us/lifecycle/products/sql-server-2022','20280111')
		) T(Name,LifecycleUrl,MainstreamEndDate)
)
UPDATE P 
	SET P.LifecycleUrl = T.LifecycleUrl,
	P.MainstreamEndDate = T.MainstreamEndDate
FROM #Parsed P
JOIN T ON P.Name = T.Name

/* Staging complete, perform update */

BEGIN TRAN

TRUNCATE TABLE dbo.BuildReference
INSERT INTO dbo.BuildReference(
		BuildReferenceVersion, /* dbatools version date */
		BuildReferenceUpdated, /* date updated in DBADash */
		Version,
		Name,
		CU,
		SP,
		KBList,
		SupportedUntil,	
		Major,
		Minor,
		Build,
		Revision,
		LatestVersion,
		LatestVersionPatchLevel,
		SPBehind,
		CUBehind,
		IsCurrentBuild,
		LifecycleUrl,
		MainstreamEndDate
)
SELECT 	BuildReferenceVersion,
		GETUTCDATE(),
		Version,
		Name,
		CU,
		SP,
		KBList,
		SupportedUntil,	
		Major,
		Minor,
		Build,
		Revision,
		LatestVersion,
		LatestVersionPatchLevel,
		SPBehind,
		CUBehind,
		IsCurrentBuild,
		LifecycleUrl,
		MainstreamEndDate
FROM #Parsed

COMMIT