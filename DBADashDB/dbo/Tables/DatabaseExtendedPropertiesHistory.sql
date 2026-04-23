CREATE TABLE [dbo].[DatabaseExtendedPropertiesHistory] (
    [DatabaseID]   INT            NOT NULL,
    [Name]         SYSNAME        NOT NULL,
    [Value]        NVARCHAR(MAX)  NULL,
    [NewValue]     NVARCHAR(MAX)  NULL,
    [ValidFrom]    DATETIME2(2)   NOT NULL,
    [ValidTo]      DATETIME2(2)   NOT NULL,
    CONSTRAINT [PK_DatabaseExtendedPropertiesHistory] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [Name] ASC, [ValidTo] ASC),
    CONSTRAINT [FK_DatabaseExtendedPropertiesHistory_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
)
