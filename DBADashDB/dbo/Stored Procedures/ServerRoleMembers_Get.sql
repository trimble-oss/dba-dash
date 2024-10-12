CREATE PROC dbo.ServerRoleMembers_Get(
	@InstanceIDs IDs READONLY,
	@IsDisabled BIT=NULL,
	@TypeDesc NVARCHAR(60)=NULL,
	@Type CHAR(1) = NULL,
	@ServerRole NVARCHAR(128)='sysadmin',
	@Login NVARCHAR(MAX)=NULL,
	@InstanceDisplayName NVARCHAR(MAX)=NULL
)
AS
/* Base query into temp table */
SELECT I.InstanceDisplayName AS Instance,
		SP.name AS Login,
		SP.type AS Type,
		SP.type_desc AS [Type Description],
		SP.is_disabled AS [Is Disabled],
		SP.create_date AS [Created Date],
		SP.modify_date AS [Modified Date]
INTO #T
FROM dbo.ServerPrincipals SP
JOIN dbo.Instances I ON SP.InstanceID = I.InstanceID
WHERE EXISTS(
		SELECT 1 
		FROM dbo.ServerRoleMembers SRM
		JOIN dbo.ServerPrincipals R ON SRM.role_principal_id = R.principal_id
		WHERE SP.principal_id = SRM.member_principal_id
		AND R.type = 'R'
		AND R.name= @ServerRole
		AND SRM.member_principal_id = SP.principal_id
		AND SRM.InstanceID = I.InstanceID
		AND R.InstanceID = I.InstanceID
		)
AND EXISTS(SELECT 1 
		FROM @InstanceIDs T 
		WHERE T.ID = I.InstanceID
		)
AND (SP.is_disabled = @IsDisabled OR @IsDisabled IS NULL)
AND (SP.type_desc = @TypeDesc OR @TypeDesc IS NULL)
AND (SP.type = @Type OR @Type IS NULL)
AND (SP.name = @Login OR @Login IS NULL)
AND (I.InstanceDisplayName = @InstanceDisplayName OR @InstanceDisplayName IS NULL)
OPTION(RECOMPILE)

/* Raw data */
SELECT Instance,
       Login,
       Type,
       [Type Description],
       [Is Disabled],
       [Created Date],
       [Modified Date]
FROM #T

/* Dynamic pivot by instance */
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT	T.Login,
		T.Type,
		T.[Type Description]
		' + (SELECT ',MAX(CASE WHEN T.Instance = ' + QUOTENAME(T.Instance,'''') + ' AND  T.[Is Disabled] =1 THEN ''D'' WHEN T.Instance = ' + QUOTENAME(T.Instance,'''') + ' AND T.[Is Disabled]=0 THEN ''A'' ELSE ''-'' END) AS ' + QUOTENAME(T.Instance) 
		FROM #T T
		GROUP BY T.Instance
		ORDER BY T.Instance
		FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)') + '
FROM #T T
GROUP BY	T.Login,
			T.Type,
			T.[Type Description]
ORDER BY T.Login
OPTION(RECOMPILE)'

EXEC sp_executesql @SQL

/* Dynamic pivot by login */
SET @SQL = N'
SELECT	T.Instance
		' + (SELECT ',MAX(CASE WHEN T.Login = ' + QUOTENAME(T.Login,'''') + ' AND  T.[Is Disabled] =1 THEN ''D'' WHEN T.Login = ' + QUOTENAME(T.Login,'''') + ' AND T.[Is Disabled]=0 THEN ''A'' ELSE ''-'' END) AS ' + QUOTENAME(T.Login) 
		FROM #T T
		GROUP BY T.Login
		ORDER BY T.Login
		FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)') + '
FROM #T T
GROUP BY T.Instance
ORDER BY T.Instance
OPTION(RECOMPILE)'

EXEC sp_executesql @SQL


GO