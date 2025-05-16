CREATE FUNCTION dbo.SplitWaitResource(@WaitResource NVARCHAR(256))
RETURNS TABLE 
AS
/* 
	Take wait resource (e.g. sys.dm_exec_requests) and split it into the relevant components (database id, file id, page id etc)
*/
RETURN
WITH wr AS (
/*  Get places of characters in string to simplify substring expressions
	sc = semi colon, 
	osb = open square bracket 
	csb = close square bracket
	ocb = open curly bracket
	ccb = close curly bracket
*/
	SELECT CHARINDEX(':',@WaitResource) sc1,
			CHARINDEX(':',@WaitResource,CHARINDEX(':',@WaitResource)+1) sc2,
			CHARINDEX(':',@WaitResource,CHARINDEX(':',@WaitResource,CHARINDEX(':',@WaitResource)+1)+1) sc3,
			CHARINDEX(':',@WaitResource, CHARINDEX(':',@WaitResource,CHARINDEX(':',@WaitResource,CHARINDEX(':',@WaitResource)+1)+1)+1) sc4,
			LEN(@WaitResource) lenwr,
			CHARINDEX('[',@WaitResource) osb1,
			CHARINDEX('(',@WaitResource) ocb1,
			CHARINDEX(')',@WaitResource) ccb1,
			CHARINDEX(' ',@WaitResource) spc1
)
, wr2 AS (
	SELECT CASE WHEN @WaitResource LIKE '[0-9]%:%:%' THEN 'PAGE'
				WHEN wr.sc1>0 THEN SUBSTRING(@WaitResource,0,wr.sc1) 
				WHEN wr.spc1>0 THEN SUBSTRING(@WaitResource,0,wr.spc1)
				ELSE NULL
				END AS wait_resource_type,			
		TRY_CAST(CASE WHEN @WaitResource LIKE '[0-9]%:%:%' THEN SUBSTRING(@WaitResource,0,wr.sc1) 
			 WHEN @WaitResource LIKE 'PAGE: [0-9]%:%:%' 
					OR @WaitResource LIKE 'OBJECT: [0-9]%:%:%' 
					OR @WaitResource LIKE 'KEY: [0-9]%:%' 
					OR @WaitResource LIKE 'RID: %:%:%:%'
						THEN SUBSTRING(@WaitResource,wr.sc1+2,wr.sc2-wr.sc1-2) 
		ELSE NULL END AS INT) AS wait_database_id,
		TRY_CAST(CASE WHEN @WaitResource LIKE '[0-9]%:%:%' THEN SUBSTRING(@WaitResource,wr.sc1+1,wr.sc2-wr.sc1-1) 
			WHEN @WaitResource LIKE 'PAGE: [0-9]%:%:%' 
				OR @WaitResource LIKE 'RID: %:%:%:%'
					THEN SUBSTRING(@WaitResource,wr.sc2+1,wr.sc3-wr.sc2-1) 
			ELSE NULL END AS INT)AS wait_file_id,
		TRY_CAST(CASE WHEN @WaitResource LIKE '[0-9]%:%:%(%)' THEN SUBSTRING(@WaitResource,wr.sc2+1,wr.ocb1-1-wr.sc2) 
			WHEN @WaitResource LIKE '[0-9]%:%:%' THEN SUBSTRING(@WaitResource,wr.sc2+1,wr.lenwr-wr.sc2) 
			WHEN @WaitResource LIKE 'PAGE: [0-9]%:%:%' 
				OR @WaitResource LIKE 'RID: %:%:%:%'
				THEN SUBSTRING(@WaitResource,wr.sc3+1,ISNULL(NULLIF(wr.sc4,0)-1,wr.lenwr)-wr.sc3) 
			ELSE NULL END AS INT) AS wait_page_id,
		TRY_CAST(CASE WHEN @WaitResource LIKE 'OBJECT: [0-9]%:%:%' THEN SUBSTRING(@WaitResource,wr.sc2+1,wr.sc3-wr.sc2-1) ELSE NULL END AS INT) AS wait_object_id,
		TRY_CAST(CASE WHEN @WaitResource LIKE 'OBJECT: [0-9]%:%:%' THEN SUBSTRING(@WaitResource,wr.sc3+1,ISNULL(NULLIF(wr.osb1,0)-2,wr.lenwr)-wr.sc3) ELSE NULL END AS INT) AS wait_index_id,
		TRY_CAST(CASE WHEN @WaitResource LIKE 'KEY: [0-9]%:%(%)%' THEN SUBSTRING(@WaitResource,wr.sc2+1,wr.ocb1-wr.sc2-2)  ELSE NULL END AS BIGINT) AS wait_hobt,
		CASE WHEN @WaitResource LIKE 'KEY: [0-9]%:%(%)%' THEN SUBSTRING(@WaitResource,wr.ocb1+1,wr.ccb1-wr.ocb1-1)  ELSE NULL END AS wait_hash,
		TRY_CAST(CASE WHEN @WaitResource LIKE 'RID: %:%:%:%' THEN SUBSTRING(@WaitResource,wr.sc4+1,wr.lenwr-wr.sc4) ELSE NULL END AS INT) AS wait_slot,
		CASE WHEN @WaitResource LIKE '%[[]COMPILE]' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS wait_is_compile
	FROM wr		
)
SELECT wr2.wait_resource_type,
       wr2.wait_database_id,
       wr2.wait_file_id,
       wr2.wait_page_id,
       wr2.wait_object_id,
       wr2.wait_index_id,
       wr2.wait_hobt,
       wr2.wait_hash,
       wr2.wait_slot,
       wr2.wait_is_compile,
	   CASE WHEN wr2.wait_page_id % 8088 =0 OR wr2.wait_page_id=1 THEN 'PFS' 
			WHEN wr2.wait_page_id % 511232 =0 OR wr2.wait_page_id=2 THEN 'GAM' 
			WHEN (wr2.wait_page_id-1) % 511232 =0 OR wr2.wait_page_id=3 THEN 'SGAM' 
			WHEN wr2.wait_page_id IS NOT NULL THEN 'Other'
	   ELSE NULL END AS page_type
FROM wr2
