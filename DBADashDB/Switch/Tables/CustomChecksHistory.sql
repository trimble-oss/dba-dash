CREATE TABLE [Switch].[CustomChecksHistory] (
    [InstanceID]   INT            NOT NULL,
    [Test]         NVARCHAR (128) NOT NULL,
    [Context]      NVARCHAR (128) NOT NULL,
    [Status]       TINYINT        NOT NULL,
    [Info]         NVARCHAR (MAX) NULL,
    [SnapshotDate] DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_DBACustomChecksHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [Test] ASC, [Context] ASC, [SnapshotDate] ASC)
);

