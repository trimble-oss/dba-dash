CREATE PROC XE.XETraceTemplate_Upd(
	@UserID INT,
	@Name NVARCHAR(128),
	@Definition NVARCHAR(MAX)
)
AS
IF EXISTS(	SELECT 1
			FROM XE.XETraceTemplate
			WHERE UserID = @UserID
			AND Name = @Name
		)
BEGIN
	UPDATE XE.XETraceTemplate
		SET Definition = @Definition,
			ModifiedDate = SYSUTCDATETIME()
	WHERE UserID = @UserID
	AND Name = @Name
END
ELSE
BEGIN
	INSERT INTO XE.XETraceTemplate(
			UserID,
			Name,
			Definition
	)
	VALUES(	@UserID,
			@Name,
			@Definition
			)
END
