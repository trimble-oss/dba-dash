CREATE PROC dbo.UserReportMetaData_Upd(
	@ProcedureName NVARCHAR(128),
	@PropertyName SYSNAME,
	@value SQL_VARIANT
)
AS
DECLARE @ObjectName SYSNAME
DECLARE @ObjectID INT
DECLARE @SchemaName SYSNAME = 'UserReport'

SELECT	@ObjectName = p.name, 
		@ObjectID = p.object_id
FROM sys.procedures p
WHERE p.object_id = OBJECT_ID(@ProcedureName)
AND schema_id = SCHEMA_ID('UserReport')

IF @ObjectID IS NULL
BEGIN
	RAISERROR('Invalid report @ProcedureName specified',11,1)
	RETURN
END

IF EXISTS(SELECT 1 
		  FROM sys.extended_properties p 
		  WHERE p.class=1
		  AND p.name = @PropertyName 
		  AND p.major_id = @ObjectID
		  ) 
	AND @value IS NULL
BEGIN
	EXEC sp_dropextendedproperty @name = @PropertyName,  
									@level0type = N'Schema', 
									@level0name = @SchemaName,
									@level1type = N'Procedure', 
									@level1name = @ObjectName
END
ELSE IF EXISTS(SELECT 1 
		  FROM sys.extended_properties p 
		  WHERE p.class=1
		  AND p.name = @PropertyName 
		  AND p.major_id = @ObjectID
		  )
BEGIN
	EXEC sp_updateextendedproperty @name = @PropertyName,
									@value = @value,    
									@level0type = N'Schema', 
									@level0name = @SchemaName,
									@level1type = N'Procedure', 
									@level1name = @ObjectName
END
ELSE
BEGIN
	EXEC sp_addextendedproperty @name = @PropertyName,
									@value = @value,    
									@level0type = N'Schema', 
									@level0name = @SchemaName,
									@level1type = N'Procedure', 
									@level1name = @ObjectName
END