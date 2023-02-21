CREATE TABLE dbo.Corruption (
    DatabaseID INT NOT NULL,
    SourceTable TINYINT NOT NULL,
    UpdateDate DATETIME2(3) NOT NULL,
    AckDate DATETIME2(3) NULL,
    CountOfRows INT NULL,
    CONSTRAINT PK_Corruption PRIMARY KEY CLUSTERED (DatabaseID ASC, SourceTable ASC),
    CONSTRAINT FK_Corruption_Databases FOREIGN KEY (DatabaseID) REFERENCES dbo.Databases (DatabaseID)
);