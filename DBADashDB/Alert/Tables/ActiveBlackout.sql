/*
	Blackout periods in effect for each instance, as of the start of the current alert run.  Rebuilt by Alert.ApplicableInstances_Upd.
	AlertKeyPattern is matched against the rule's AlertKey with LIKE (% for collections paused from the GUI).
	Usually empty or tiny - checking it is much cheaper than evaluating Alert.IsBlackoutPeriod for every rule x instance.
*/
CREATE TABLE Alert.ActiveBlackout(
	InstanceID INT NOT NULL,
	AlertKeyPattern NVARCHAR(256) NOT NULL,
	CONSTRAINT PK_Alert_ActiveBlackout PRIMARY KEY(InstanceID,AlertKeyPattern)
)
