CREATE PROC [dbo].[ServerPermissions_Upd](@ServerPermissions dbo.ServerPermissions READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='ServerPermissions'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	DELETE dbo.ServerPermissions
	WHERE InstanceID=@InstanceID
	INSERT INTO dbo.ServerPermissions
	(
		InstanceID,
		class,
        class_desc,
        major_id,
        minor_id,
        grantee_principal_id,
        grantor_principal_id,
        type,
        permission_name,
        state,
        state_desc
	)
	SELECT @InstanceID,
		class,
        class_desc,
        major_id,
        minor_id,
        grantee_principal_id,
        grantor_principal_id,
        type,
        permission_name,
        state,
        state_desc
	FROM @ServerPermissions
	

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT
END