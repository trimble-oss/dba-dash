CREATE PROC dbo.PlanForcingLog_Add(
	@InstanceID INT,
	@database_name NVARCHAR(128),
	@log_type VARCHAR(20),
	@user_name NVARCHAR(256),
	@query_id BIGINT,
	@plan_id BIGINT,
	@object_name NVARCHAR(128),
	@query_sql_text NVARCHAR(MAX),
	@query_hash BINARY(8),
	@query_plan_hash BINARY(8),
	@notes NVARCHAR(MAX),
	@MessageGroupID UNIQUEIDENTIFIER
)
AS
INSERT INTO dbo.PlanForcingLog(InstanceID,database_name,log_type,user_name,query_id,plan_id,object_name,query_sql_text,query_hash,query_plan_hash,notes,MessageGroupID)
VALUES(@InstanceID,@database_name,@log_type,@user_name,@query_id,@plan_id,@object_name,@query_sql_text,@query_hash,@query_plan_hash,@notes,@MessageGroupID)
