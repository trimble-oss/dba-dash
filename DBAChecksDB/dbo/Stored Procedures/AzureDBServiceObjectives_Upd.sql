CREATE PROC [dbo].[AzureDBServiceObjectives_Upd](@AzureDBServiceObjectives dbo.AzureDBServiceObjectives READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='AzureDBServiceObjectives'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN

	INSERT INTO dbo.AzureDBServiceObjectives
	(
		InstanceID,	
	    edition,
	    service_objective,
	    elastic_pool_name,
		ValidFrom
	)
	SELECT @InstanceID,
		   edition,
           service_objective,
           elastic_pool_name,
		   @SnapshotDate AS ValidFrom
	FROM @AzureDBServiceObjectives
	WHERE NOT EXISTS(SELECT 1 FROM dbo.AzureDBServiceObjectives WHERE InstanceID = @InstanceID)

	IF @@ROWCOUNT=0
	BEGIN
		UPDATE ASO
			SET ASO.edition = t.edition,
			ASO.service_objective  = t.service_objective,
			ASO.elastic_pool_name = t.elastic_pool_name,
			ASO.ValidFrom = @SnapshotDate
		OUTPUT DELETED.InstanceID,DELETED.edition,DELETED.service_objective,DELETED.elastic_pool_name,Inserted.edition,Inserted.service_objective,Inserted.elastic_pool_name,Deleted.ValidFrom,@SnapshotDate
			INTO dbo.AzureDBServiceObjectivesHistory(InstanceID,edition,service_objective,elastic_pool_name,new_edition,new_service_objective,new_elastic_pool_name,ValidFrom,ValidTo)
		FROM dbo.AzureDBServiceObjectives ASO
		CROSS JOIN @AzureDBServiceObjectives t
		WHERE ASO.InstanceID = @InstanceID
		AND NOT (ASO.edition = t.edition
				AND ASO.service_objective = t.service_objective
				AND EXISTS(SELECT ASO.elastic_pool_name INTERSECT SELECT t.elastic_pool_name)
				)

	END

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END