CREATE TABLE dbo.BuildReference(
	BuildReferenceVersion DATETIME NOT NULL,
	BuildReferenceUpdated DATETIME NOT NULL,
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
	LifecycleUrl NVARCHAR(MAX) NULL,
	MainstreamEndDate DATETIME NULL,
	CONSTRAINT PK_BuildReference PRIMARY KEY(Version)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_BuildReference_Major_Minor_Build_Revision ON dbo.BuildReference(Major,Minor,Build,Revision)