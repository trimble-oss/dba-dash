CREATE FUNCTION SQLVersionName(@EditionID SYSNAME, @ProductVersion SYSNAME)
RETURNS TABLE
AS
RETURN
SELECT 	CASE WHEN @EditionID = 1674378470 AND @ProductVersion LIKE '%.%' THEN 'Azure ' + SUBSTRING(@ProductVersion,1,CHARINDEX('.',@ProductVersion)-1)
			WHEN @ProductVersion LIKE '9.%' THEN 'SQL 2005' 
			WHEN @ProductVersion LIKE '10.0%' THEN 'SQL 2008' 
			WHEN @ProductVersion LIKE '10.5%' THEN 'SQL 2008 R2'
			WHEN @ProductVersion LIKE '11.%' THEN 'SQL 2012'
			WHEN @ProductVersion LIKE '12.%' THEN 'SQL 2014'
			WHEN @ProductVersion LIKE '13.%' THEN 'SQL 2016'
			WHEN @ProductVersion LIKE '14.%' THEN 'SQL 2017'
			WHEN @ProductVersion LIKE '15.%' THEN 'SQL 2019'
			ELSE @ProductVersion END AS SQLVersionName