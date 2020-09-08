CREATE TYPE [dbo].[DatabasePermissions] AS TABLE (
    [database_id]          INT            NOT NULL,
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
    [schema_name]          NVARCHAR (128) NULL,
    [object_name]          NVARCHAR (128) NULL,
    [column_name]          NVARCHAR (128) NULL);

