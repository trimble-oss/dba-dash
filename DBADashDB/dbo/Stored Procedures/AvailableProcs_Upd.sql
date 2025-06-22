CREATE PROC dbo.AvailableProcs_Upd(
	@AvailableProcs dbo.AvailableProcs READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='AvailableProcs'

BEGIN TRAN
DELETE dbo.AvailableProcs 
WHERE InstanceID = @InstanceID;

WITH RemoveDupe AS (
	/* Case sensitive collations *could* intruduce duplicates */
	SELECT 	database_name,
			from_master,
			schema_name,
			object_name,
			parameters,
			COUNT(*) OVER(PARTITION BY object_name,schema_name) cnt
	FROM @AvailableProcs
)
INSERT INTO dbo.AvailableProcs(
	InstanceID,
	database_name,
	from_master,
	schema_name,
	object_name,
	parameters
)
SELECT 	@InstanceID,
		database_name,
		from_master,
		schema_name,
		object_name,
		parameters
FROM RemoveDupe
WHERE cnt=1

COMMIT

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                            @Reference = @Ref,
	                            @SnapshotDate = @SnapshotDate