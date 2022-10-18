CREATE PROC dbo.Instance_ShowInSummary_Upd(
	@InstanceID INT,
	@ShowInSummary BIT
)
AS
UPDATE dbo.Instances
	SET ShowInSummary = @ShowInSummary
WHERE InstanceID = @InstanceID

