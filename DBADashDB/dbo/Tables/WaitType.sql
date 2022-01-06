CREATE TABLE dbo.WaitType (
    WaitTypeID     SMALLINT      IDENTITY (1, 1) NOT NULL,
    WaitType       NVARCHAR (60) NOT NULL,
    IsCriticalWait BIT           CONSTRAINT DF_WaitTyppe_IsCriticalWait DEFAULT ((0)) NOT NULL,
	Description VARCHAR(2000) NULL,
    IsExcluded BIT NOT NULL CONSTRAINT DF_WaitType_IsExcluded DEFAULT(0),
    CONSTRAINT PK_WaitType PRIMARY KEY CLUSTERED (WaitTypeID ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_WaitType_WaitType
    ON dbo.WaitType(WaitType ASC);
