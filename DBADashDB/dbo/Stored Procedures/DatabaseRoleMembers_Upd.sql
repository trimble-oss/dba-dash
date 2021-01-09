CREATE PROC [dbo].[DatabaseRoleMembers_Upd](@DatabaseRoleMembers dbo.DatabaseRoleMembers READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='DatabaseRoleMembers'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	DELETE DRM 
	FROM dbo.DatabaseRoleMembers DRM 
	WHERE EXISTS(SELECT 1 
				FROM dbo.Databases D 
				WHERE D.DatabaseID = DRM.DatabaseID 
				AND D.InstanceID = @InstanceID)

	INSERT INTO dbo.DatabaseRoleMembers
	(
	    DatabaseID,
	    role_principal_id,
	    member_principal_id
	)
	SELECT	D.DatabaseID,
			DRM.role_principal_id,
			DRM.member_principal_id
	FROM @DatabaseRoleMembers DRM 
	JOIN dbo.Databases D ON DRM.database_id = D.database_id
	WHERE D.InstanceID = @InstanceID

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT
END