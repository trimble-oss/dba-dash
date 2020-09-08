CREATE TABLE [dbo].[DatabaseRoleMembers] (
    [DatabaseID]          INT NOT NULL,
    [role_principal_id]   INT NOT NULL,
    [member_principal_id] INT NOT NULL,
    CONSTRAINT [PK_DatabaseRoleMembers] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [role_principal_id] ASC, [member_principal_id] ASC),
    CONSTRAINT [FK_DatabaseRoleMembers_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);

