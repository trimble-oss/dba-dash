CREATE TABLE [dbo].[DatabaseExtendedProperties] (
    [DatabaseID]   INT            NOT NULL,
    [Name]         SYSNAME        NOT NULL,
    [Value]        NVARCHAR(MAX)  NULL,
    [ValidFrom]    DATETIME2(2)   NOT NULL,
    CONSTRAINT [PK_DatabaseExtendedProperties] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [Name] ASC),
    CONSTRAINT [FK_DatabaseExtendedProperties_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
)
