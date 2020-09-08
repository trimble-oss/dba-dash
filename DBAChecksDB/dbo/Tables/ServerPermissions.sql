CREATE TABLE [dbo].[ServerPermissions] (
    [InstanceID]           INT            NOT NULL,
    [class]                TINYINT        NOT NULL,
    [class_desc]           NVARCHAR (60)  NULL,
    [major_id]             INT            NOT NULL,
    [minor_id]             INT            NOT NULL,
    [grantee_principal_id] INT            NOT NULL,
    [grantor_principal_id] INT            NOT NULL,
    [type]                 CHAR (4)       NOT NULL,
    [permission_name]      NVARCHAR (128) NULL,
    [state]                CHAR (1)       NOT NULL,
    [state_desc]           NVARCHAR (60)  NULL,
    CONSTRAINT [PK_ServerPermissions] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [type] ASC, [class] ASC, [major_id] ASC, [minor_id] ASC, [grantee_principal_id] ASC, [grantor_principal_id] ASC),
    CONSTRAINT [FK_ServerPermissions_Instance] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

