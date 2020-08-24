CREATE PROC [dbo].[Partitions_Add]
AS
DECLARE @DaysInFuture INT=14
EXEC dbo.DailyPartitions_Add @TableName = 'Waits',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'IOStats',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'CPU',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'BlockingSnapshot',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'AzureDBResourceStats',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'AzureDBElasticPoolResourceStats',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'ObjectExecutionStats',@DaysInFuture=@DaysInFuture