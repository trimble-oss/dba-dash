CREATE PROC [dbo].[DatabasePrincipals_Upd](@DatabasePrincipals dbo.DatabasePrincipals READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='DatabasePrincipals'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN

	DELETE DP 
	FROM dbo.DatabasePrincipals DP
	WHERE EXISTS(
				SELECT * 
				FROM dbo.Databases D 
				WHERE D.DatabaseID = DP.DatabaseID
				AND D.InstanceID = @InstanceID
				)



	INSERT INTO dbo.DatabasePrincipals
	(
       DatabaseID,
       name,
       principal_id,
       type,
       type_desc,
       default_schema_name,
       create_date,
       modify_date,
       owning_principal_id,
       sid,
       is_fixed_role,
       authentication_type,
       authentication_type_desc,
       default_language_name,
       default_language_lcid,
       allow_encrypted_value_modifications
	)
	SELECT D.DatabaseID,
		   DP.name,
		   DP.principal_id,
		   DP.type,
		   DP.type_desc,
		   DP.default_schema_name,
		   DP.create_date,
		   DP.modify_date,
		   DP.owning_principal_id,
		   DP.sid,
		   DP.is_fixed_role,
		   DP.authentication_type,
		   DP.authentication_type_desc,
		   DP.default_language_name,
		   DP.default_language_lcid,
		   DP.allow_encrypted_value_modifications
	FROM @DatabasePrincipals DP
	JOIN dbo.Databases D ON DP.database_id = D.database_id
	WHERE D.InstanceID = @InstanceID
	

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT
END