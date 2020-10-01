
CREATE PROC [Report].[SlowQueriesDetail](
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@ObjectName SYSNAME=NULL,
	@ClientHostName NVARCHAR(128)=NULL,
	@ConnectionID SYSNAME=NULL,
	@ClientAppName SYSNAME=NULL,
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DurationFromSec BIGINT=NULL,
	@DurationToSec BIGINT=NULL,
	@Text NVARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@Top INT = 30
)
AS
EXEC [dbo].[SlowQueriesDetail_Get]
	@FromDate=@FromDate,
	@ToDate=@ToDate,
	@ObjectName=@ObjectName,
	@ClientHostName=@ClientHostName,
	@ConnectionID=@ConnectionID,
	@ClientAppName=@ClientAppName,
	@InstanceIDs=@InstanceIDs,
	@DurationFromSec=@DurationFromSec,
	@DurationToSec=@DurationToSec,
	@Text=@Text,
	@DatabaseName=@DatabaseName,
	@Top=@Top