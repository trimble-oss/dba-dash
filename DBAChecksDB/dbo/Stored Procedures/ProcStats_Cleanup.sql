CREATE PROC [dbo].[ProcStats_Cleanup](@DaysToKeep INT)
AS
EXEC [dbo].[PartitionTable_Cleanup] @TableName='ProcStats',@DaysToKeep=@DaysToKeep