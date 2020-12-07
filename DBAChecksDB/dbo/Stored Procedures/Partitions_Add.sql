CREATE PROC [dbo].[Partitions_Add]
AS
DECLARE @DaysInFuture INT=14
EXEC dbo.DailyPartitions_Add @TableName = 'Waits',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'CPU',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'BlockingSnapshot',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'AzureDBResourceStats',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'AzureDBElasticPoolResourceStats',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'ObjectExecutionStats',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'DBIOStats',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'SlowQueries',@DaysInFuture=@DaysInFuture
EXEC dbo.DailyPartitions_Add @TableName = 'CustomChecksHistory',@DaysInFuture=@DaysInFuture

DECLARE @MonthsInFuture INT=1
EXEC dbo.MonthlyPartitions_Add @TableName = 'AzureDBElasticPoolResourceStats_60MIN',@MonthsInFuture=@MonthsInFuture
EXEC dbo.MonthlyPartitions_Add @TableName = 'AzureDBResourceStats_60MIN',@MonthsInFuture=@MonthsInFuture
EXEC dbo.MonthlyPartitions_Add @TableName = 'CPU_60MIN',@MonthsInFuture=@MonthsInFuture
EXEC dbo.MonthlyPartitions_Add @TableName = 'DBIOStats_60MIN',@MonthsInFuture=@MonthsInFuture
EXEC dbo.MonthlyPartitions_Add @TableName = 'Waits_60MIN',@MonthsInFuture=@MonthsInFuture
EXEC dbo.MonthlyPartitions_Add @TableName = 'ObjectExecutionStats_60MIN',@MonthsInFuture=@MonthsInFuture