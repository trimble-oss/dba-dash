CREATE FUNCTION dbo.SplitStrings
(
   @List       NVARCHAR(MAX),
   @Delimiter  NVARCHAR(255)
)
RETURNS TABLE
WITH SCHEMABINDING
AS
   RETURN 
   (  
      SELECT Item = y.i.value('(./text())[1]', 'nvarchar(4000)')
      FROM 
      ( 
        SELECT x = CONVERT(XML, '<i>' 
          + REPLACE(@List, @Delimiter, '</i><i>') 
          + '</i>').query('.')
      ) AS a CROSS APPLY x.nodes('i') AS y(i)
   );