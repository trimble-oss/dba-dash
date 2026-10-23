CREATE FUNCTION dbo.EditionMemoryLimit(
	@EditionID BIGINT,
	@Edition NVARCHAR(128),
	@EngineEdition INT,
	@ProductMajorVersion INT,
	@ProductVersion NVARCHAR(128),
	@ProductLevel NVARCHAR(128),
	@PhysicalMemoryKB BIGINT,
	@MaxServerMemoryMB BIGINT
)
RETURNS TABLE
AS
RETURN
/*
	Buffer pool cap for the Database Engine, by edition and version.
	Microsoft documents this as "Maximum memory for buffer pool", not as a cap on max server memory.
	From SQL Server 2012, max server memory also includes clerks outside the buffer pool, so a max server
	memory value above the buffer pool cap is valid.

	Limits follow the edition scale tables (buffer pool):
	  Express: 1 GB through SQL Server 2014; 1,410 MB from SQL Server 2016
	  Web: no cap on SQL Server 2008; 64 GB from SQL Server 2008 R2 through SQL Server 2022
	  Business Intelligence: 64 GB (2012), 128 GB (2014)
	  Workgroup: 3 GB (2005), 4 GB (2008 and 2008 R2)
	  Standard (and SQL Server 2025 Standard Developer): no cap through SQL Server 2008; 64 GB on 2008 R2 and 2012;
	    128 GB on 2014-2022; 256 GB on 2025 and later
	  Enterprise on SQL Server 2008 R2: 2 TB. Datacenter, Developer, Evaluation, and other Enterprise editions: no cap
	Columnstore segment cache and memory-optimized data have separate caps from SQL Server 2016 SP1 (and 2017+).
	Azure SQL is not evaluated; capacity comes from the service tier.
	Status values match DBADashGUI.DBADashStatus.DBADashStatusEnum (Warning=2, NA=3, OK=4, Information=7).
	Information is used when physical memory exceeds the cap by no more than the larger of 4 GB and 10% of the cap
	(typical operating-system headroom). A larger gap is a warning.
*/
WITH base AS (
	SELECT COALESCE(@ProductMajorVersion, TRY_CAST(LEFT(@ProductVersion, NULLIF(CHARINDEX('.', @ProductVersion + '.'), 0) - 1) AS INT)) AS MajorVersion,
		CASE WHEN @ProductVersion LIKE '10.5%' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Is2008R2
),
classified AS (
	SELECT b.MajorVersion,
		b.Is2008R2,
		CASE WHEN @EngineEdition IN (5,6,8,9,11,12) OR @EditionID IN (1674378470, -1461570097, 1994083197) THEN 'Azure'
			WHEN @EditionID IN (-1592396055, -133711905) OR @Edition LIKE '%Express%' THEN 'Express'
			WHEN @EditionID = 1293598313 OR @Edition LIKE 'Web Edition%' THEN 'Web'
			WHEN @EditionID = 284895786 OR @Edition LIKE '%Business Intelligence%' THEN 'BI'
			WHEN @EditionID = 1333529388 OR @Edition LIKE '%Workgroup%' THEN 'Workgroup'
			WHEN @EditionID IN (-1534726760, -1785266663) OR @Edition LIKE '%Standard%' THEN 'Standard'
			WHEN b.Is2008R2 = 1 AND (@EditionID = 1804890536 OR (@EditionID IS NULL AND @Edition LIKE 'Enterprise Edition%')) THEN 'Enterprise2008R2'
			WHEN @EditionID IN (1804890536, 1872460670, 610778273, -2117995310)
				OR @Edition LIKE '%Enterprise%'
				OR @Edition LIKE '%Developer%'
				OR @Edition LIKE '%Evaluation%'
				OR @Edition LIKE '%Datacenter%' THEN 'Unlimited'
			ELSE 'Unknown' END AS EditionGroup,
		CASE WHEN b.MajorVersion > 13 OR (b.MajorVersion = 13 AND @ProductLevel LIKE 'SP%') THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasExtendedLimits
	FROM base b
),
limits AS (
	SELECT c.EditionGroup,
		c.MajorVersion,
		CASE c.EditionGroup
			WHEN 'Express' THEN CASE WHEN c.MajorVersion >= 13 THEN 1410 WHEN c.MajorVersion IS NOT NULL THEN 1024 END
			WHEN 'Web' THEN CASE WHEN c.Is2008R2 = 1 OR c.MajorVersion >= 11 THEN 65536 END
			WHEN 'BI' THEN CASE WHEN c.MajorVersion IS NULL THEN NULL WHEN c.MajorVersion <= 11 THEN 65536 ELSE 131072 END
			WHEN 'Workgroup' THEN CASE WHEN c.MajorVersion IS NULL THEN NULL WHEN c.MajorVersion <= 9 THEN 3072 ELSE 4096 END
			WHEN 'Standard' THEN CASE
					WHEN c.MajorVersion >= 17 THEN 262144
					WHEN c.MajorVersion >= 12 THEN 131072
					WHEN c.MajorVersion = 11 OR c.Is2008R2 = 1 THEN 65536
				END
			WHEN 'Enterprise2008R2' THEN 2097152
		END AS BufferPoolLimitMB,
		CASE WHEN c.HasExtendedLimits = 1 AND c.EditionGroup = 'Standard' THEN 32768
			WHEN c.HasExtendedLimits = 1 AND c.EditionGroup = 'Web' THEN 16384
			WHEN c.HasExtendedLimits = 1 AND c.EditionGroup = 'Express' THEN 352
		END AS ColumnstoreLimitMB,
		CASE WHEN c.HasExtendedLimits = 1 AND c.EditionGroup = 'Standard' THEN 32768
			WHEN c.HasExtendedLimits = 1 AND c.EditionGroup = 'Express' THEN 352
		END AS InMemoryLimitMB,
		CASE c.EditionGroup
			WHEN 'Express' THEN 'Express edition'
			WHEN 'Web' THEN 'Web edition'
			WHEN 'BI' THEN 'Business Intelligence edition'
			WHEN 'Workgroup' THEN 'Workgroup edition'
			WHEN 'Standard' THEN CASE WHEN @EditionID = -1785266663 OR @Edition LIKE '%Developer%' THEN 'Standard Developer edition' ELSE 'Standard edition' END
			WHEN 'Enterprise2008R2' THEN 'Enterprise edition'
			WHEN 'Unlimited' THEN CASE
					WHEN @Edition LIKE '%Datacenter%' THEN 'Datacenter edition'
					WHEN @EditionID = -2117995310 OR (@Edition LIKE '%Developer%' AND @Edition NOT LIKE '%Standard%') THEN 'Developer edition'
					WHEN @EditionID = 610778273 OR @Edition LIKE '%Evaluation%' THEN 'Evaluation edition'
					ELSE 'Enterprise edition' END
			ELSE 'This edition' END AS EditionLabel
	FROM classified c
),
fmt AS (
	SELECT l.EditionGroup,
		l.MajorVersion,
		l.BufferPoolLimitMB,
		l.EditionLabel,
		CASE WHEN @PhysicalMemoryKB IS NULL THEN NULL ELSE CONCAT(FORMAT(@PhysicalMemoryKB / POWER(1024.0, 2), 'N1'), ' GB') END AS PhysicalText,
		CASE WHEN l.BufferPoolLimitMB IS NULL THEN NULL
			WHEN l.BufferPoolLimitMB >= 1048576 THEN CONCAT(FORMAT(l.BufferPoolLimitMB / 1048576.0, 'N0'), ' TB')
			WHEN l.BufferPoolLimitMB >= 10240 THEN CONCAT(FORMAT(l.BufferPoolLimitMB / 1024.0, 'N0'), ' GB')
			WHEN l.BufferPoolLimitMB >= 1024 THEN CONCAT(FORMAT(l.BufferPoolLimitMB / 1024.0, 'N1'), ' GB')
			ELSE CONCAT(FORMAT(l.BufferPoolLimitMB, 'N0'), ' MB') END AS LimitText,
		CASE WHEN l.ColumnstoreLimitMB IS NULL THEN NULL
			WHEN l.ColumnstoreLimitMB >= 1024 THEN CONCAT(FORMAT(l.ColumnstoreLimitMB / 1024.0, 'N0'), ' GB')
			ELSE CONCAT(FORMAT(l.ColumnstoreLimitMB, 'N0'), ' MB') END AS ColumnstoreText,
		CASE WHEN l.InMemoryLimitMB IS NULL THEN NULL
			WHEN l.InMemoryLimitMB >= 1024 THEN CONCAT(FORMAT(l.InMemoryLimitMB / 1024.0, 'N0'), ' GB')
			ELSE CONCAT(FORMAT(l.InMemoryLimitMB, 'N0'), ' MB') END AS InMemoryText,
		CASE WHEN @MaxServerMemoryMB IS NULL THEN 'Max server memory is not available.'
			WHEN @MaxServerMemoryMB >= 2147483647 THEN 'Max server memory has not been configured. The default allows SQL Server to target as much memory as the edition permits.'
			WHEN @PhysicalMemoryKB IS NOT NULL AND CAST(@MaxServerMemoryMB AS BIGINT) * 1024 > @PhysicalMemoryKB + 524288 THEN CONCAT('Max server memory is set to ',
				CASE WHEN @MaxServerMemoryMB >= 10240 THEN CONCAT(FORMAT(@MaxServerMemoryMB / 1024.0, 'N0'), ' GB')
					WHEN @MaxServerMemoryMB >= 1024 THEN CONCAT(FORMAT(@MaxServerMemoryMB / 1024.0, 'N1'), ' GB')
					ELSE CONCAT(FORMAT(@MaxServerMemoryMB, 'N0'), ' MB') END,
				', which is above the ', FORMAT(@PhysicalMemoryKB / POWER(1024.0, 2), 'N1'), ' GB of physical memory. SQL Server cannot use more memory than the server has, so this setting does not reserve RAM for the operating system.')
			WHEN @PhysicalMemoryKB >= 16777216
				AND CAST(@MaxServerMemoryMB AS BIGINT) * 1024 <= @PhysicalMemoryKB / 2
				AND (l.BufferPoolLimitMB IS NULL OR @MaxServerMemoryMB < l.BufferPoolLimitMB * 0.8)
				THEN CONCAT('Max server memory is set to ',
				CASE WHEN @MaxServerMemoryMB >= 10240 THEN CONCAT(FORMAT(@MaxServerMemoryMB / 1024.0, 'N0'), ' GB')
					WHEN @MaxServerMemoryMB >= 1024 THEN CONCAT(FORMAT(@MaxServerMemoryMB / 1024.0, 'N1'), ' GB')
					ELSE CONCAT(FORMAT(@MaxServerMemoryMB, 'N0'), ' MB') END,
				' of ', FORMAT(@PhysicalMemoryKB / POWER(1024.0, 2), 'N1'), ' GB physical memory (', FORMAT(CAST(@MaxServerMemoryMB AS BIGINT) * 1024.0 / @PhysicalMemoryKB, 'P0'), '). That is half of the installed RAM or less. A lower setting can be appropriate when several instances share the server or other applications need memory.')
			ELSE CONCAT('Max server memory is set to ',
				CASE WHEN @MaxServerMemoryMB >= 10240 THEN CONCAT(FORMAT(@MaxServerMemoryMB / 1024.0, 'N0'), ' GB')
					WHEN @MaxServerMemoryMB >= 1024 THEN CONCAT(FORMAT(@MaxServerMemoryMB / 1024.0, 'N1'), ' GB')
					ELSE CONCAT(FORMAT(@MaxServerMemoryMB, 'N0'), ' MB') END,
				CASE WHEN l.BufferPoolLimitMB IS NULL THEN '.'
					WHEN @MaxServerMemoryMB <= l.BufferPoolLimitMB THEN ', which is within the edition buffer pool limit. Max server memory does not raise that cap.'
					ELSE '. That is above the buffer pool limit, which is valid: from SQL Server 2012, max server memory also covers clerks outside the buffer pool.' END)
			END AS MaxServerMemoryText,
		CASE WHEN l.EditionGroup IN ('Azure', 'Unknown') THEN CAST(0 AS BIT)
			WHEN l.MajorVersion IS NULL AND l.EditionGroup <> 'Unlimited' THEN CAST(0 AS BIT)
			WHEN @PhysicalMemoryKB IS NULL THEN CAST(0 AS BIT)
			ELSE CAST(1 AS BIT) END AS IsEvaluated,
		CAST(CASE WHEN l.BufferPoolLimitMB IS NULL THEN NULL ELSE CAST(l.BufferPoolLimitMB AS BIGINT) * 1024 END AS BIGINT) AS LimitKB,
		CAST(CASE WHEN l.BufferPoolLimitMB IS NULL THEN NULL WHEN l.BufferPoolLimitMB * 0.1 > 4096 THEN l.BufferPoolLimitMB * 102.4 ELSE 4194304 END AS BIGINT) AS HeadroomKB
	FROM limits l
)
SELECT CASE WHEN f.IsEvaluated = 0 THEN 3
		WHEN f.BufferPoolLimitMB IS NULL THEN 4
		WHEN @PhysicalMemoryKB <= f.LimitKB THEN 4
		WHEN @PhysicalMemoryKB <= f.LimitKB + f.HeadroomKB THEN 7
		ELSE 2 END AS PhysicalMemoryLimitStatus,
	CASE WHEN f.EditionGroup = 'Azure' THEN 'Memory limits are not evaluated for Azure SQL. Capacity is determined by the service tier.'
		WHEN f.IsEvaluated = 0 AND @PhysicalMemoryKB IS NULL AND f.EditionGroup NOT IN ('Azure', 'Unknown') THEN 'Physical memory is unknown, so it was not compared with the edition buffer pool limit.'
		WHEN f.IsEvaluated = 0 THEN 'Edition or version is unknown, so the buffer pool memory limit was not evaluated.'
		WHEN f.BufferPoolLimitMB IS NULL THEN CONCAT(f.EditionLabel, ' does not impose a buffer pool memory limit.', CHAR(10), CHAR(10), f.MaxServerMemoryText)
		WHEN @PhysicalMemoryKB <= f.LimitKB THEN CONCAT('Physical memory is ', f.PhysicalText, ', within the ', f.EditionLabel, ' buffer pool limit of ', f.LimitText, '.', CHAR(10), CHAR(10), f.MaxServerMemoryText)
		ELSE CONCAT(
			'Physical memory is ', f.PhysicalText, ', ',
			CASE WHEN @PhysicalMemoryKB <= f.LimitKB + f.HeadroomKB THEN 'slightly above' ELSE 'above' END,
			' the ', f.EditionLabel, ' buffer pool limit of ', f.LimitText, ' for this version of SQL Server.',
			CASE WHEN @PhysicalMemoryKB <= f.LimitKB + f.HeadroomKB THEN ' The difference is in the range often left for the operating system.' ELSE '' END,
			CHAR(10), CHAR(10),
			'The Database Engine will not use the memory above that limit for the buffer pool.',
			CASE WHEN f.ColumnstoreText IS NULL THEN ''
				WHEN f.InMemoryText IS NULL THEN CONCAT(' Columnstore segment cache (up to ', f.ColumnstoreText, ') can use additional memory.')
				ELSE CONCAT(' Columnstore segment cache (up to ', f.ColumnstoreText, ') and memory-optimized data (up to ', f.InMemoryText, ') can use additional memory.') END,
			CHAR(10), CHAR(10),
			f.MaxServerMemoryText,
			CHAR(10), CHAR(10),
			'The limit applies to this instance. Other SQL Server instances on the same server have their own limits.')
		END AS PhysicalMemoryLimitMessage
FROM fmt f
