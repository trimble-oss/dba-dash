CREATE TYPE [dbo].[ServerRoleMembers] AS TABLE (
    [role_principal_id]   INT NOT NULL,
    [member_principal_id] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([role_principal_id] ASC,[member_principal_id] ASC));

