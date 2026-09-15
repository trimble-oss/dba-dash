/*
	Recomputed deadlock signatures, sent by the service's signature recompute (see
	dbo.DeadlockSignatureRecompute_Upd).  Keyed on the primary key of dbo.Deadlocks.

	Signature arrives as the familiar "0x..." hex string, as it does in dbo.Deadlocks.  NULL when the graph didn't
	parse - the row keeps its signature but is marked as looked at.
*/
CREATE TYPE dbo.DeadlockSignatures AS TABLE (
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	Signature VARCHAR(18) NULL,
	SignatureVersion TINYINT NOT NULL,
	PRIMARY KEY (InstanceID, EventTime, DeadlockHash)
);
