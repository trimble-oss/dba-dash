CREATE PROC [dbo].[DatabasePermissions_Upd](@DatabasePermissions dbo.DatabasePermissions READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='DatabasePermissions'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	DELETE DP 
	FROM dbo.DatabasePermissions DP
	WHERE EXISTS(SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DP.DatabaseID)



	INSERT INTO dbo.DatabasePermissions
	(
		   DatabaseID,
		   class,
           class_desc,
           major_id,
           minor_id,
           grantee_principal_id,
           grantor_principal_id,
           type,
           permission_name,
           state,
           state_desc,
           schema_name,
           object_name,
           column_name 
	)
	SELECT D.DatabaseID,
           DP.class,
           DP.class_desc,
           DP.major_id,
           DP.minor_id,
           DP.grantee_principal_id,
           DP.grantor_principal_id,
           DP.type,
           DP.permission_name,
           DP.state,
           DP.state_desc,
           DP.schema_name,
           DP.object_name,
           DP.column_name 
	FROM @DatabasePermissions DP
	JOIN dbo.Databases D ON DP.database_id = D.database_id
	WHERE D.InstanceID = @InstanceID
	

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT
END