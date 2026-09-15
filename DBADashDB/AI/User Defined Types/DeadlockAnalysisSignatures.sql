/*
	Recomputed signatures for stored AI analyses, sent by the service's signature recompute (see
	AI.DeadlockAnalysisSignatureRecompute_Upd).

	Signature arrives as the familiar "0x..." hex string, as it does everywhere else.  NULL when the graph didn't
	parse - the analysis keeps its signature but is marked as looked at.
*/
CREATE TYPE AI.DeadlockAnalysisSignatures AS TABLE (
	DeadlockAnalysisID BIGINT NOT NULL PRIMARY KEY,
	Signature VARCHAR(18) NULL,
	SignatureVersion TINYINT NOT NULL
);
