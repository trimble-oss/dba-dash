CREATE TABLE [dbo].[OSLoadedModules] (
    [InstanceID]      INT            NOT NULL,
    [base_address]    VARBINARY (8)  NULL,
    [file_version]    VARCHAR (256)  NULL,
    [product_version] VARCHAR (256)  NULL,
    [debug]           BIT            NULL,
    [patched]         BIT            NULL,
    [prerelease]      BIT            NULL,
    [private_build]   BIT            NULL,
    [special_build]   BIT            NULL,
    [language]        INT            NULL,
    [company]         NVARCHAR (256) NOT NULL,
    [description]     NVARCHAR (256) NOT NULL,
    [name]            NVARCHAR (255) NOT NULL,
    [Status]          TINYINT        NOT NULL,
    CONSTRAINT [PK_OSLoadedModules] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [name] ASC),
    CONSTRAINT [FK_OSLoadedModules_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);




GO
CREATE NONCLUSTERED INDEX [IX_OSLoadedModules_Company]
    ON [dbo].[OSLoadedModules]([company] ASC)
    INCLUDE([name]);


GO
CREATE NONCLUSTERED INDEX [FIX_OSLoadedModules_Status]
    ON [dbo].[OSLoadedModules]([InstanceID] ASC, [Status] ASC)
    INCLUDE([name], [company], [description]) WHERE ([Status]<(4));

