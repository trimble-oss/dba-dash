CREATE PROC [Report].[DBFiles_Get](@InstanceIDs VARCHAR(MAX)=NULL,@FilterLevel TINYINT=2)
AS
DECLARE @Critical BIT 
DECLARE @Warning BIT 
DECLARE @OK BIT 
DECLARE @NA BIT 
SELECT @Critical = CASE WHEN @FilterLevel IN(1,2,4) THEN 1 ELSE 0 END,
	@Warning = CASE WHEN @FilterLevel IN(2,4) THEN 1 ELSE 0 END,
	@OK = CASE WHEN @FilterLevel =4 THEN 1 ELSE 0 END,
	@NA = CASE WHEN @FilterLevel=4 THEN 1 ELSE 0 END


EXEC dbo.DBFiles_Get @InstanceIDs = @InstanceIDs,
                     @IncludeCritical = @Critical, 
                     @IncludeWarning = @Warning, 
                     @IncludeNA = @NA, 
                     @IncludeOK = @OK