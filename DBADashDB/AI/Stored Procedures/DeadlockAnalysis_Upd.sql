/*
	Stores the analysis for a deadlock pattern, replacing any previous answer for the same model and
	payload version.

	Replacing rather than versioning: a second answer to the same question from the same model on the
	same input is not history worth keeping, and re-analysing is exactly what someone does when they
	did not trust the first answer.
*/
CREATE PROC AI.DeadlockAnalysis_Upd
(
	@Signature VARCHAR(18),
	@Model NVARCHAR(128),
	@PayloadVersion VARCHAR(30),
	@Analysis NVARCHAR(MAX),
	@InstanceID INT = NULL
)
AS
SET NOCOUNT ON

/* Callers pass the signature as the familiar "0x..." hex string; store it in its 8-byte binary form
   (style 1 parses the leading 0x). */
DECLARE @SignatureBin BINARY(8) = CONVERT(BINARY(8), @Signature, 1);

UPDATE AI.DeadlockAnalysis
SET Analysis = @Analysis,
	InstanceID = @InstanceID,
	GeneratedUtc = SYSUTCDATETIME()
WHERE Signature = @SignatureBin
AND Model = @Model
AND PayloadVersion = @PayloadVersion

IF @@ROWCOUNT = 0
BEGIN
	/* Two clients analysing the same pattern at once is possible and harmless - the loser of the
	   race keeps the row the winner wrote, which is an equally good answer to the same question.
	   UPDLOCK,HOLDLOCK takes the key range on the unique index so the loser waits for the winner and
	   then finds the row, rather than both passing the check and one failing on the index. */
	INSERT INTO AI.DeadlockAnalysis (Signature, Model, PayloadVersion, Analysis, InstanceID)
	SELECT @SignatureBin, @Model, @PayloadVersion, @Analysis, @InstanceID
	WHERE NOT EXISTS
	(
		SELECT 1
		FROM AI.DeadlockAnalysis WITH(UPDLOCK,HOLDLOCK)
		WHERE Signature = @SignatureBin
		AND Model = @Model
		AND PayloadVersion = @PayloadVersion
	)
END
