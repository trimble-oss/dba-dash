CREATE PROC [Report].[Blocking](@BlockingSnapshotID INT,@blocking_session_id INT)
AS
EXEC [dbo].[Blocking_Get] @BlockingSnapshotID = @BlockingSnapshotID,@blocking_session_id=@blocking_session_id