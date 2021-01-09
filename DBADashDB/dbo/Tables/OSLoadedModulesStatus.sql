CREATE TABLE [dbo].[OSLoadedModulesStatus] (
    [NAME]        NVARCHAR (256) NOT NULL,
    [Company]     NVARCHAR (256) NOT NULL,
    [Description] NVARCHAR (256) NOT NULL,
    [Status]      TINYINT        NOT NULL,
    CONSTRAINT [PK_OSLoadedModulesStatus] PRIMARY KEY CLUSTERED ([NAME] ASC, [Company] ASC, [Description] ASC)
);

