
CREATE PROC [Report].[SlowQueriesSummary](
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@ObjectName SYSNAME=NULL,
	@ClientHostName NVARCHAR(128)=NULL,
	@ConnectionID SYSNAME=NULL,
	@ClientAppName SYSNAME=NULL,
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DurationFromSec BIGINT=NULL,
	@DurationToSec BIGINT=NULL,
	@GroupBy VARCHAR(50)='ConnectionID',
	@Text NVARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@UserName SYSNAME=NULL,
	@Top INT=20
)
AS
EXEC dbo.SlowQueriesSummary_Get @FromDate = @FromDate,
                            @ToDate = @ToDate,
                            @ObjectName = @ObjectName,
                            @ClientHostName = @ClientHostName,
                            @ConnectionID = @ConnectionID,
                            @ClientAppName = @ClientAppName,
                            @InstanceIDs = @InstanceIDs,
                            @DurationFromSec = @DurationFromSec,
                            @DurationToSec = @DurationToSec,
                            @GroupBy = @GroupBy,
                            @Text = @Text,
                            @DatabaseName = @DatabaseName,
                            @UserName =@UserName,
                            @Top = @Top