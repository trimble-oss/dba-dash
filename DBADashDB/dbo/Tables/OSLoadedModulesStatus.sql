CREATE TABLE [dbo].[OSLoadedModulesStatus] (
    [Name]        NVARCHAR (256) NOT NULL,
    [Company]     NVARCHAR (256) NOT NULL,
    [Description] NVARCHAR (256) NOT NULL,
    [Status]      TINYINT        NOT NULL,
    CONSTRAINT [PK_OSLoadedModulesStatus] PRIMARY KEY CLUSTERED ([Name] ASC, [Company] ASC, [Description] ASC)
);

