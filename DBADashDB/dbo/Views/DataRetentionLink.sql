/*
	Tables whose retention follows another table's.  A linked table has no row of its own in dbo.DataRetention - it is
	kept for as long as the table it follows, and dbo.DataRetentionEffective gives it that table's retention.

	For tables that are meaningless without the other: the coverage tables the stats reports divide by, and the
	child rows of a parent that is shown with them.  Not every child table follows its parent - DeadlockXml is kept
	for less time than Deadlocks on purpose, as the XML is large and the parsed rows are what the reports use.

	Both tables must be partitioned on the same period (see dbo.PartitionConfiguration), so the same retention purges
	the same partitions.
*/
CREATE VIEW dbo.DataRetentionLink
AS
SELECT	SchemaName,
		TableName,
		SourceSchemaName,
		SourceTableName
FROM (
VALUES	('dbo','QueryStatsCollection','dbo','QueryStats'),
		('dbo','QueryStatsCollection_60MIN','dbo','QueryStats_60MIN'),
		('dbo','LatchStatsCollection','dbo','LatchStats'),
		('dbo','LatchStatsCollection_60MIN','dbo','LatchStats_60MIN'),
		('dbo','SpinlockStatsCollection','dbo','SpinlockStats'),
		('dbo','SpinlockStatsCollection_60MIN','dbo','SpinlockStats_60MIN'),
		('dbo','DeadlockProcesses','dbo','Deadlocks'),
		('dbo','DeadlockResources','dbo','Deadlocks'),
		('dbo','RunningQueriesCursors','dbo','RunningQueries')
	) T(SchemaName,TableName,SourceSchemaName,SourceTableName)
