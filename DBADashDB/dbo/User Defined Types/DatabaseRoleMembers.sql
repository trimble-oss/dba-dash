CREATE TYPE [dbo].[DatabaseRoleMembers] AS TABLE (
    [database_id]         INT NOT NULL,
    [role_principal_id]   INT NOT NULL,
    [member_principal_id] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([database_id] ASC, [role_principal_id] ASC, [member_principal_id] ASC));

