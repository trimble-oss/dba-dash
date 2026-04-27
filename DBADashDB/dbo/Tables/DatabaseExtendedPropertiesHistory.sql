CREATE TABLE [dbo].[DatabaseExtendedPropertiesHistory] (
    [InstanceID]   INT            NOT NULL,
    [DatabaseID]   INT            NOT NULL,
    [Name]         SYSNAME        NOT NULL,
    [Value]        NVARCHAR(MAX)  NULL,
    [NewValue]     NVARCHAR(MAX)  NULL,
    [ValidFrom]    DATETIME2(2)   NOT NULL,
    [ValidTo]      DATETIME2(2)   NOT NULL,
    CONSTRAINT [PK_DatabaseExtendedPropertiesHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DatabaseID] ASC, [Name] ASC, [ValidTo] ASC)
)
