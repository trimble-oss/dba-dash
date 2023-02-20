/*
----------------------------------------------------------
	 ____   ____     _      ____               _          
	|  _ \ | __ )   / \    |  _ \   __ _  ___ | |__       
	| | | ||  _ \  / _ \   | | | | / _` |/ __|| '_ \      
	| |_| || |_) |/ ___ \  | |_| || (_| |\__ \| | | |     
	|____/ |____//_/   \_\ |____/  \__,_||___/|_| |_|     
                                                        
	SQL Server Monitoring by David Wiseman			     
	Copyright 2022 Trimble, Inc.                          
	https://dbadash.com                                   
                                                              
	**Instructions**
	Run this query on the SQL instance ({instance_name}) to display information from msdb.dbo.suspect_pages, sys.dm_hadr_auto_page_repair & sys.dm_db_mirroring_auto_page_repair

	Use DBCC CHECKDB regularly to validate that your databases are free of corruption.  When corruption is detected take steps to repair the corruption and identify the root cause.

----------------------------------------------------------
*/
SELECT	'suspect_pages' AS Source,
		DB_NAME(sp.database_id) as database_name,
		sp.database_id,
		sp.file_id,
		mf.physical_name,
		sp.page_id,
		sp.event_type,
		CASE sp.event_type 
			WHEN 1 THEN '823 or 824 error (excluding bad checksum or torn page)'
			WHEN 2 THEN 'Bad checksum'
			WHEN 3 THEN 'Torn page'
			WHEN 4 THEN 'Restored'
			WHEN 5 THEN 'Repaired'
			WHEN 7 THEN 'Deallocated by DBCC'
			ELSE CAST(sp.event_type AS VARCHAR(50)) + ' - ?'
			END AS event_type_desc,
		sp.error_count,
		sp.last_update_date,
		'DBCC PAGE(' + CAST(sp.database_id as VARCHAR(MAX)) + ',' + CAST(sp.file_id AS VARCHAR(MAX)) + ',' + CAST(sp.page_id AS VARCHAR(MAX)) +') WITH TABLERESULTS' as [DBCC PAGE],
		'DBCC CHECKDB(' + CAST(sp.database_id as VARCHAR(MAX)) + ') WITH NO_INFOMSGS, ALL_ERRORMSGS --,DATA_PURITY,EXTENDED_LOGICAL_CHECKS,MAXDOP=?' as [DBCC CHECKDB]
FROM msdb.dbo.suspect_pages sp
LEFT JOIN sys.master_files mf ON sp.database_id =mf.database_id AND sp.file_id =mf.file_id

SELECT @@ROWCOUNT suspect_pages_row_count,1000 AS max_rows,@@ROWCOUNT*100.0/1000.0 AS pct_max_rows

IF OBJECT_ID('sys.dm_hadr_auto_page_repair') IS NOT NULL
BEGIN
	SELECT	'sys.dm_hadr_auto_page_repair' AS Source,
			DB_NAME(r.database_id) as database_name,
			r.database_id,
			r.file_id,
			mf.physical_name,
			r.error_type,
			CASE r.error_type 
				WHEN -1 THEN 'All hardware 823 errors'
				WHEN 1 THEN '824 errors other than a bad checksum or a torn page (such as a bad page ID)'
				WHEN 2 THEN 'Bad checksum'
				WHEN 3 THEN 'Torn page'
				ELSE CAST(r.error_type AS VARCHAR(50)) + ' - ?' END as error_type_desc,
			r.modification_time,
			r.page_id,
			r.page_status,
			CASE r.page_status 
				WHEN 2 THEN 'Queued for request from partner.'
				WHEN 3 THEN 'Request sent to partner.'
				WHEN 4 THEN 'Page was successfully repaired.'
				WHEN 5 THEN 'The page could not be repaired during the last attempt/ Automatic page repair will attempt to repair the page again.'
				ELSE CAST(r.page_status AS VARCHAR(50)) + ' - ?' END as page_status_desc,
			'DBCC PAGE(' + CAST(r.database_id as VARCHAR(MAX)) + ',' + CAST(r.file_id AS VARCHAR(MAX)) + ',' + CAST(r.page_id AS VARCHAR(MAX)) +') WITH TABLERESULTS' as [DBCC PAGE],
			'DBCC CHECKDB(' + CAST(r.database_id as VARCHAR(MAX)) + ') WITH NO_INFOMSGS, ALL_ERRORMSGS --,DATA_PURITY,EXTENDED_LOGICAL_CHECKS,MAXDOP=?' as [DBCC CHECKDB]
	FROM sys.dm_hadr_auto_page_repair r
	LEFT JOIN sys.master_files mf ON r.database_id =mf.database_id AND r.file_id =mf.file_id
	ORDER BY r.modification_time DESC
END

IF OBJECT_ID('sys.dm_db_mirroring_auto_page_repair') IS NOT NULL
BEGIN
	SELECT	'sys.dm_db_mirroring_auto_page_repair' AS Source,
			DB_NAME(r.database_id) as database_name,
			r.database_id,
			r.file_id,
			mf.physical_name,
			r.error_type,
			CASE r.error_type 
				WHEN -1 THEN 'All hardware 823 errors'
				WHEN 1 THEN '824 errors other than a bad checksum or a torn page (such as a bad page ID)'
				WHEN 2 THEN 'Bad checksum'
				WHEN 3 THEN 'Torn page'
				ELSE CAST(r.error_type AS VARCHAR(50)) + ' - ?' END as error_type_desc,
			r.modification_time,
			r.page_id,
			r.page_status,
			CASE r.page_status 
				WHEN 2 THEN 'Queued for request from partner.'
				WHEN 3 THEN 'Request sent to partner.'
				WHEN 4 THEN 'Queued for automatic page repair (response received from partner)'
				WHEN 5 THEN 'Automatic page repair succeeded and the page should be usable.'
				WHEN 6 THEN 'Irreparable. This indicates that an error occurred during page-repair attempt, for example, because the page is also corrupted on the partner, the partner is disconnected, or a network problem occurred. This state is not terminal; if corruption is encountered again on the page, the page will be requested again from the partner.'
				ELSE CAST(r.page_status AS VARCHAR(50)) + ' - ?' END as page_status_desc,
			'DBCC PAGE(' + CAST(r.database_id as VARCHAR(MAX)) + ',' + CAST(r.file_id AS VARCHAR(MAX)) + ',' + CAST(r.page_id AS VARCHAR(MAX)) +') WITH TABLERESULTS' as [DBCC PAGE],
			'DBCC CHECKDB(' + CAST(r.database_id as VARCHAR(MAX)) + ') WITH NO_INFOMSGS, ALL_ERRORMSGS --,DATA_PURITY,EXTENDED_LOGICAL_CHECKS,MAXDOP=?' as [DBCC CHECKDB]
	FROM sys.dm_db_mirroring_auto_page_repair r
	LEFT JOIN sys.master_files mf ON r.database_id =mf.database_id AND r.file_id =mf.file_id
	ORDER BY r.modification_time DESC
END

