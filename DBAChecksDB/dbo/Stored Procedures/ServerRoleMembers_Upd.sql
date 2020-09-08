CREATE PROC ServerRoleMembers_Upd(@ServerRoleMembers dbo.ServerRoleMembers READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='ServerRoleMembers'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	DELETE dbo.ServerRoleMembers
	WHERE InstanceID=@InstanceID
	INSERT INTO dbo.ServerRoleMembers
	(
	    InstanceID,
	    role_principal_id,
	    member_principal_id
	)
	SELECT @InstanceID,
			role_principal_id,
           member_principal_id
	FROM @ServerRoleMembers

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT
END