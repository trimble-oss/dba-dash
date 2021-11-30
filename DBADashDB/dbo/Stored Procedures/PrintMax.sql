--From: https://weblogs.asp.net/bdill/sql-server-print-max
CREATE PROCEDURE dbo.PrintMax(
	@iInput NVARCHAR(MAX)
)
AS
BEGIN
 -- nothing we can do with null data
 IF (@iInput IS NULL) BEGIN
  RETURN;
 END;

 -- This procedure was created to properly print
 -- nvarchar(max) since the print statement can
 -- only handle NVARCHAR(4000), we break the
 -- input down into 4000 byte blocks and print
 -- upto the last linebreak before the 4000 byte cutoff
 DECLARE @ReversedData NVARCHAR(MAX)
  ,@LineBreakIndex INT
  ,@SearchLength INT;

 -- if the search length is less than the first occurance
 -- of a line break, the data will be printed with a line break
 -- at the SearchLength position even though there should not be
 -- a break in the data there.
 SET @SearchLength = 4000;

 -- only loop while the input is greater than the search length
 WHILE (LEN(@iInput) > @SearchLength) BEGIN
  -- obtain and reverse the input upto the search length
  SET @ReversedData = LEFT(@iInput, @SearchLength);
  SET @ReversedData = REVERSE(@ReversedData);

  -- determine the position of the first line break for this piece of data
  SET @LineBreakIndex = CHARINDEX(CHAR(10) + CHAR(13), @ReversedData);

  -- print the input only showing data upto the line break
  -- the original linebreak will not be displayed
  PRINT LEFT(@iInput, @SearchLength - @LineBreakIndex + 1);

  -- resize the input removing the data that was displayed and the line break.
  SET @iInput = RIGHT(@iInput, LEN(@iInput) - @SearchLength + @LineBreakIndex - 1);
 END;

 -- if there is any data remaining, print it
 IF (LEN(@iInput) > 0) BEGIN
  PRINT @iInput;
 END;
END;
GO