/*
	Instances each tag-scoped alert rule applies to (single tag or multiple tags).
	Rebuilt by Alert.ApplicableInstances_Upd at the start of each alert run so the alert procs seek the matching instances
	instead of probing tags for every rule x instance.  Rules for all instances or a specific instance aren't stored here.
*/
CREATE TABLE Alert.RuleInstanceMatch(
	RuleID INT NOT NULL,
	InstanceID INT NOT NULL,
	CONSTRAINT PK_Alert_RuleInstanceMatch PRIMARY KEY(RuleID,InstanceID)
)
