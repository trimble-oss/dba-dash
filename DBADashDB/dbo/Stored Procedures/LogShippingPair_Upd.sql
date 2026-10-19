CREATE PROC dbo.LogShippingPair_Upd(
	@PrimaryInstanceID INT,
	@SecondaryInstanceID INT,
	@Delete BIT=0
)
AS
/*
	Adds a log shipping primary/secondary instance pair.  @Delete=1 removes the pair.
*/
SET NOCOUNT ON

IF @PrimaryInstanceID = @SecondaryInstanceID
BEGIN
	RAISERROR('Primary and secondary instance must be different',11,1)
	RETURN
END

IF @Delete=1
BEGIN
	DELETE dbo.LogShippingPairs
	WHERE PrimaryInstanceID = @PrimaryInstanceID
	AND SecondaryInstanceID = @SecondaryInstanceID
END
ELSE
BEGIN
	INSERT INTO dbo.LogShippingPairs(PrimaryInstanceID,SecondaryInstanceID)
	SELECT @PrimaryInstanceID,@SecondaryInstanceID
	WHERE NOT EXISTS(
			SELECT 1
			FROM dbo.LogShippingPairs WITH(UPDLOCK,HOLDLOCK)
			WHERE PrimaryInstanceID = @PrimaryInstanceID
			AND SecondaryInstanceID = @SecondaryInstanceID
			)
END
