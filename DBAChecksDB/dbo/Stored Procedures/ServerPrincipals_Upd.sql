CREATE PROC ServerPrincipals_Upd(@ServerPrincipals dbo.ServerPrincipals READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='ServerPrincipals'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	DELETE dbo.ServerPrincipals
	WHERE InstanceID=@InstanceID
	INSERT INTO dbo.ServerPrincipals
	(
		InstanceID,
	    name,
	    principal_id,
	    sid,
	    type,
	    type_desc,
	    is_disabled,
	    create_date,
	    modify_date,
	    default_database_name,
	    default_language_name,
	    credential_id,
	    owning_principal_id,
	    is_fixed_role
	)
	SELECT @InstanceID,
			name,
           principal_id,
           sid,
           type,
           type_desc,
           is_disabled,
           create_date,
           modify_date,
           default_database_name,
           default_language_name,
           credential_id,
           owning_principal_id,
           is_fixed_role
	FROM @ServerPrincipals
	

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT
END