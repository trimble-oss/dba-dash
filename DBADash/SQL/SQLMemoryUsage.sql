SET TRAN ISOLATION LEVEL READ UNCOMMITTED
DECLARE @Threshold BIGINT=1024
DECLARE @PagesKB VARCHAR(MAX) = CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_os_memory_clerks'),'pages_kb','ColumnId') IS NOT NULL THEN 'c.pages_kb' ELSE 'c.single_pages_kb + c.multi_pages_kb' END
DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = '
SELECT type,
	SUM(' + @PagesKB + ') pages_kb,
	SUM(virtual_memory_reserved_kb) AS virtual_memory_reserved_kb,
	SUM(virtual_memory_committed_kb) AS virtual_memory_committed_kb,
	SUM(awe_allocated_kb) AS awe_allocated_kb,
	SUM(shared_memory_reserved_kb) AS shared_memory_reserved_kb,
	SUM(shared_memory_committed_kb) AS shared_memory_committed_kb
FROM sys.dm_os_memory_clerks c
WHERE (' + @PagesKB + '> @Threshold
OR c.virtual_memory_committed_kb>@Threshold
OR c.virtual_memory_reserved_kb>@Threshold
OR c.awe_allocated_kb>@Threshold
OR c.shared_memory_committed_kb>@Threshold
OR c.shared_memory_reserved_kb>@Threshold)
GROUP BY type'

EXEC sp_executesql @SQL,N'@Threshold BIGINT',@Threshold