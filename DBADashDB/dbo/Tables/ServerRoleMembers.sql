CREATE TABLE [dbo].[ServerRoleMembers] (
    [InstanceID]          INT NOT NULL,
    [role_principal_id]   INT NOT NULL,
    [member_principal_id] INT NOT NULL,
    CONSTRAINT [PK_ServerRoleMembers] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [role_principal_id] ASC, [member_principal_id] ASC),
    CONSTRAINT [FK_ServerRoleMembers_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

