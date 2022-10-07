CREATE TABLE dbo.OSLoadedModulesStatus (
    ID INT IDENTITY(1,1) CONSTRAINT PK_OSLoadedModulesStatus PRIMARY KEY CLUSTERED,
    Name NVARCHAR(256) NOT NULL,
    Company NVARCHAR(256) NOT NULL,
    Description NVARCHAR(256) NOT NULL,
    Status TINYINT NOT NULL,
    Notes NVARCHAR(256) NULL,
    IsSystem BIT NOT NULL CONSTRAINT DF_OSLoadedModuleStatus_IsSystem DEFAULT(1)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_OSLoadedModulesStatus_Name_Company_Description ON  dbo.OSLoadedModulesStatus(Name ASC, Company ASC, Description ASC)
