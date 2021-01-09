CREATE PROC [Report].[ServerLoginSummary](
	@InstanceIDs VARCHAR(MAX)=NULL,
	@PrincipalType VARCHAR(MAX)='S,U,G,C,K',
	@IncludeDisabled BIT=0,
	@LoginName SYSNAME=NULL
)
AS

DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;


WITH DBPermissions AS (
	SELECT	D.InstanceID,
			DP.DatabaseID,
			DP.principal_id,
			DP.name,
			DP.type_desc,
			DP.sid,
			MAX(CASE WHEN R.name = 'db_owner' THEN 1 ELSE 0 END) as db_owner,
			MAX(CASE WHEN R.name = 'db_accessadmin' THEN 1 ELSE 0 END) as db_accessadmin,
			MAX(CASE WHEN R.name = 'db_securityadmin' THEN 1 ELSE 0 END) as db_securityadmin,
			MAX(CASE WHEN R.name = 'db_ddladmin' THEN 1 ELSE 0 END) as db_ddladmin,
			MAX(CASE WHEN R.name = 'db_backupoperator' THEN 1 ELSE 0 END) as db_backupoperator,
			MAX(CASE WHEN R.name = 'db_datareader' THEN 1 ELSE 0 END) as db_datareader,
			MAX(CASE WHEN R.name = 'db_datawriter' THEN 1 ELSE 0 END) as db_datawriter,
			MAX(CASE WHEN R.name = 'db_denydatareader' THEN 1 ELSE 0 END) as db_denydatareader,
			MAX(CASE WHEN R.name = 'db_denydatawriter' THEN 1 ELSE 0 END) as db_denydatawriter,
			MAX(CASE WHEN R.is_fixed_role=0 THEN 1 ELSE 0 END) as CustomRole,
			CASE WHEN EXISTS(SELECT 1 FROM dbo.DatabasePermissions DPer WHERE DP.DatabaseID = DPer.DatabaseID AND DPer.grantee_principal_id = DP.principal_id AND type<>'CO') THEN 1 ELSE 0 END as CustomPermissions
	FROM dbo.DatabasePrincipals DP
	JOIN dbo.Databases D ON DP.DatabaseID = D.DatabaseID
	LEFT JOIN dbo.DatabaseRoleMembers DRM ON DP.principal_id = DRM.member_principal_id AND DRM.DatabaseID = D.DatabaseID
	LEFT JOIN dbo.DatabasePrincipals R ON R.DatabaseID = D.DatabaseID AND DRM.role_principal_id = R.principal_id
	WHERE DP.type <> 'R'
	AND DP.sid IS NOT NULL
	AND D.IsActive=1
	GROUP BY DP.name,DP.type_desc,DP.sid,DP.DatabaseID,DP.principal_id,D.InstanceID
),
ServerPermissions AS (
	SELECT SP.principal_id,
		SP.InstanceID,
		I.Instance,
		SP.sid,
		SP.name,
		SP.type_desc,
		SP.create_date,
		SP.modify_date,
		SP.default_database_name,
		SP.default_language_name,
		SP.is_disabled,
		MAX(CASE WHEN R.name='sysadmin' THEN 1 ELSE 0 END) as SysAdmin,
		MAX(CASE WHEN R.name='securityadmin' THEN 1 ELSE 0 END) as SecurityAdmin,
		MAX(CASE WHEN R.name='serveradmin' THEN 1 ELSE 0 END) as ServerAdmin,
		MAX(CASE WHEN R.name='setupadmin' THEN 1 ELSE 0 END) as SetupAdmin,
		MAX(CASE WHEN R.name='processadmin' THEN 1 ELSE 0 END) as ProcessAdmin,
		MAX(CASE WHEN R.name='diskadmin' THEN 1 ELSE 0 END) as DiskAdmin,
		MAX(CASE WHEN R.name='dbcreator' THEN 1 ELSE 0 END) as DBCreator,
		MAX(CASE WHEN R.name='bulkadmin' THEN 1 ELSE 0 END) as BulkAdmin,
		MAX(CASE WHEN R.is_fixed_role=0 THEN 1 ELSE 0 END) as CustomServerRole,
		CASE WHEN EXISTS(SELECT 1 FROM dbo.ServerPermissions sPer WHERE sPer.InstanceID = SP.InstanceID AND sPer.grantee_principal_id = SP.principal_id AND sPer.type<>'COSQ') THEN 1 ELSE 0 END as CustomServerPermissions
	FROM dbo.ServerPrincipals SP
	JOIN dbo.Instances I ON SP.InstanceID = I.InstanceID
	LEFT JOIN dbo.ServerRoleMembers SRM ON SRM.InstanceID = SP.InstanceID AND SRM.member_principal_id = SP.principal_id
	LEFT JOIN dbo.ServerPrincipals R ON R.InstanceID = SRM.InstanceID AND SRM.role_principal_id = R.principal_id AND R.type='R'
	WHERE EXISTS(SELECT * FROM STRING_SPLIT(@PrincipalType,',') ss WHERE ss.value = SP.type)
	AND (SP.is_disabled = 0 OR @IncludeDisabled=1)
	AND I.IsActive=1
	AND (SP.name LIKE @LoginName OR @LoginName IS NULL)
	AND EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = SP.InstanceID)
	GROUP BY SP.name,
		SP.type_desc,
		I.Instance,
		SP.InstanceID,
		SP.principal_id,
		SP.is_disabled,
		SP.sid,
		SP.create_date,
		SP.modify_date,
		SP.default_database_name,
		SP.default_language_name
)
,DBPermissionsAgg AS (
SELECT DP.sid,
	   DP.InstanceID,
       SUM(DP.db_owner) db_owner,
       SUM(DP.db_accessadmin) db_accessadmin,
       SUM(DP.db_securityadmin) db_securityadmin,
       SUM(DP.db_ddladmin) db_ddladmin,
       SUM(DP.db_backupoperator) db_backupoperator,
       SUM(DP.db_datareader) db_datareader,
       SUM(DP.db_datawriter) db_datawriter,
       SUM(DP.db_denydatareader) db_denydatareader,
       SUM(DP.db_denydatawriter) db_denydatawriter,
       SUM(DP.CustomRole) CustomRole,
       SUM(DP.CustomPermissions) CustomPermissions,
	   COUNT(*) AS DatabaseCount
FROM DBPermissions DP
GROUP BY DP.sid, DP.InstanceID
)
SELECT SP.principal_id,
       SP.InstanceID,
	   SP.Instance,
       SP.sid,
       SP.name,
       SP.type_desc,
       SP.create_date,
       SP.modify_date,
       SP.default_database_name,
       SP.default_language_name,     
       SP.is_disabled,
       SP.SysAdmin,
       SP.SecurityAdmin,
       SP.ServerAdmin,
       SP.SetupAdmin,
       SP.ProcessAdmin,
       SP.DiskAdmin,
       SP.DBCreator,
       SP.BulkAdmin,
       SP.CustomServerRole,
       SP.CustomServerPermissions,
	   ISNULL(DP.DatabaseCount,0) as DatabaseCount,
       ISNULL(DP.db_owner,0) db_owner,
       ISNULL(DP.db_accessadmin,0) db_accessadmin ,
       ISNULL(DP.db_securityadmin,0) db_securityadmin,
       ISNULL(DP.db_ddladmin,0) db_ddladmin,
       ISNULL(DP.db_backupoperator,0) db_backupoperator,
       ISNULL(DP.db_datareader,0) db_datareader,
       ISNULL(DP.db_datawriter,0) db_datawriter,
       ISNULL(DP.db_denydatareader,0) db_denydatareader,
       ISNULL(DP.db_denydatawriter,0) db_denydatawriter,
       ISNULL(DP.CustomRole,0) CustomRole,
       ISNULL(DP.CustomPermissions,0) CustomPermissions
FROM ServerPermissions SP
LEFT JOIN DBPermissionsAgg DP ON DP.sid = SP.sid AND DP.InstanceID = SP.InstanceID
ORDER BY SP.name,SP.Instance