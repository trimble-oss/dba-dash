CREATE PROC dbo.Corruption_Ack(
	@DatabaseID INT,
	@Clear BIT=0
)
AS
UPDATE dbo.Corruption
	SET AckDate = CASE WHEN @Clear = 1 THEN NULL ELSE GETUTCDATE() END
WHERE DatabaseID = @DatabaseID

