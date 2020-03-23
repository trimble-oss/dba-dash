
CREATE PROC [dbo].[DBTuningOptions_Upd](@DBTuningOptions dbo.DBTuningOptions READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='DBTuningOptions'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN;
	WITH T AS (
		SELECT InstanceID,
               DatabaseID,
               name,
               desired_state_desc,
               actual_state_desc,
               reason_desc,
               ValidFrom 
		FROM dbo.DBTuningOptions T
		WHERE InstanceID = @InstanceID
		AND EXISTS(SELECT 1 FROM dbo.Databases D WHERE d.DatabaseID = t.DatabaseID AND D.IsActive=1 AND D.InstanceID  = @InstanceID)
	)
	MERGE T
	USING(
		SELECT d.DatabaseID, d.InstanceID, t.database_id,
                            t.name,
                            t.desired_state_desc,
                            t.actual_state_desc,
                            t.reason_desc 
		FROM @DBTuningOptions t
		JOIN dbo.Databases d ON t.database_id = d.database_id AND d.InstanceID = @InstanceID AND d.IsActive=1
		) AS S
		ON S.DatabaseID = T.DatabaseID AND S.name = t.name
	WHEN MATCHED AND (EXISTS(SELECT S.desired_state_desc EXCEPT SELECT T.desired_state_desc)
				OR EXISTS(SELECT S.actual_state_desc EXCEPT SELECT T.actual_state_desc)
				OR EXISTS(SELECT S.reason_desc EXCEPT SELECT T.reason_desc)
				)
	THEN UPDATE SET T.actual_state_desc = S.actual_state_desc,
			T.desired_state_desc = S.desired_state_desc,
			T.reason_desc = T.reason_desc,
			T.ValidFrom=@SnapshotDate
	WHEN NOT MATCHED BY SOURCE
	 THEN DELETE
	WHEN NOT MATCHED BY TARGET THEN INSERT(InstanceID,DatabaseID,name,desired_state_desc,actual_state_desc,reason_desc,ValidFrom)
		VALUES(S.InstanceID,S.DatabaseID,s.name,S.desired_state_desc,s.actual_state_desc,s.reason_desc,@SnapshotDate)
	OUTPUT ISNULL(Deleted.InstanceID,Inserted.InstanceID),
           ISNULL(Deleted.DatabaseID,Inserted.DatabaseID),
           ISNULL(Deleted.name,Inserted.name),
           Deleted.desired_state_desc,
           Deleted.actual_state_desc,
           Deleted.reason_desc,
           ISNULL(Deleted.ValidFrom,'19000101') ,
		   @SnapshotDate,
		   Inserted.actual_state_desc,
		   Inserted.desired_state_desc,
		   Inserted.reason_desc
		   INTO dbo.DBTuningOptionsHistory(InstanceID,DatabaseID,name,desired_state_desc,actual_state_desc,reason_desc,ValidFrom,ValidTo,new_actual_state_desc,new_desired_state_desc,new_reason_desc);

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END