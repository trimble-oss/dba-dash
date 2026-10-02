CREATE TABLE dbo.LatchClass (
    LatchClassID SMALLINT IDENTITY (1, 1) NOT NULL,
    LatchClass NVARCHAR (60) NOT NULL,
    Description NVARCHAR (2000) NULL,
    CONSTRAINT PK_LatchClass PRIMARY KEY CLUSTERED (LatchClassID ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_LatchClass_LatchClass
    ON dbo.LatchClass(LatchClass ASC);
