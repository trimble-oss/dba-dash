CREATE PROC dbo.Job_Diff(
	@InstanceID_A INT,
	@InstanceID_B INT
)
AS
WITH A AS(
	SELECT I.ConnectionID,
		J.name,
		J.description,
		J.category,
		J.date_modified,
		J.DDLID,
		J.SnapshotUpdatedDate
	FROM dbo.Jobs J
	JOIN dbo.Instances I ON J.InstanceID = I.InstanceID
	WHERE J.InstanceID = @InstanceID_A
	AND J.IsActive=1
)
,B as (
	SELECT I.ConnectionID,
		J.name,
		J.description,
		J.category,
		J.date_modified,
		J.DDLID,
		J.SnapshotUpdatedDate
	FROM dbo.Jobs J 
	JOIN dbo.Instances I ON J.InstanceID = I.InstanceID
	WHERE J.InstanceID = @InstanceID_B
	AND J.IsActive=1
)
SELECT CASE WHEN A.ConnectionID IS NULL THEN B.ConnectionID + ' Only'  
			WHEN B.ConnectionID IS NULL THEN A.ConnectionID + ' Only'
			WHEN A.DDLID = B.DDLID THEN '=' ELSE '<>' END as Diff,
		ISNULL(A.name,B.name) as name,
		A.description as description_A,
		B.description as description_B, 
		A.category as category_A,
		B.category as category_B, 
		A.date_modified as date_modified_A,
		b.date_modified as date_modified_B,
		A.SnapshotUpdatedDate as SnapshotUpdated_A,
		B.SnapshotUpdatedDate as SnapshotUpdated_B,
		A.DDLID as DDLID_A,
		B.DDLID as DDLID_B
FROM A 
FULL JOIN B ON A.name = B.name