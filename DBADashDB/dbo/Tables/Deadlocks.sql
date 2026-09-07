/*
	One row per deadlock.  The graph itself is in dbo.DeadlockXml so it can carry a shorter retention
	than the shredded rows - see that table for why.

	EventTime is the XE event timestamp (UTC), not the time the collection ran.

	DeadlockHash identifies the occurrence and is the third key column, so the primary key is itself
	the dedup constraint - there is no separate unique index, and no surrogate to allocate.  A re-read
	is therefore harmless by construction: overlapping polls, a cursor reset after a service restart,
	or a switch of collection source can all present the same deadlock again.

	It is a truncated SHA2_256 of the graph XML, computed in the collector over the graph as it arrived
	- whitespace normalised so that the same event read through different targets hashes the same, but
	every attribute value kept.  That is deliberately the opposite of what Signature does: the
	signature strips spids, transaction ids, timestamps and literals so that recurrences group, while
	this value has to tell two occurrences apart, so nothing volatile may be removed from it.

	Two distinct deadlocks in the same millisecond therefore cannot collide here even when they share a
	signature: their transaction ids differ, as do the per-process spid, wait time and log used, and
	the graph-internal process and lock ids, which are addresses of the underlying structures.  Byte
	identical graphs mean the same event was read twice - which is exactly what should deduplicate.

	16 bytes rather than the 8 used elsewhere for hashes (query_plan_hash, Signature).  Those group;
	this one deduplicates, so a collision would silently discard a real deadlock rather than merely
	mis-group it, and 2^128 puts that beyond consideration at any scale.  32 would too, but the value
	propagates into the key of both child tables, so the narrower one is worth having.

	Signature identifies the *shape* of the deadlock (see DBADash.Deadlock.Analysis.DeadlockSignature)
	so recurrences group as one problem.  Stored as BINARY(8) to match AI.DeadlockAnalysis.Signature,
	which lets the signature report join straight to a cached analysis.  SignatureVersion is stored so
	that a change to what goes into a signature is visible rather than silently regrouping history, and
	so a backfill has something to select on.
*/
CREATE TABLE dbo.Deadlocks(
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	Signature BINARY(8) NULL,
	SignatureVersion TINYINT NULL,
	ProcessCount SMALLINT NULL,
	VictimCount SMALLINT NULL,
	ResourceCount SMALLINT NULL,
	IsParallel BIT NULL,
	CONSTRAINT PK_Deadlocks PRIMARY KEY CLUSTERED (InstanceID ASC, EventTime ASC, DeadlockHash ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_Deadlocks(EventTime),
	CONSTRAINT FK_Deadlocks_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
);
GO
/* Drives the group-by-signature report, which is the default view of this data. */
CREATE INDEX IX_Deadlocks_Signature ON dbo.Deadlocks(InstanceID, Signature, EventTime) WITH (DATA_COMPRESSION = PAGE) ON PS_Deadlocks(EventTime);
