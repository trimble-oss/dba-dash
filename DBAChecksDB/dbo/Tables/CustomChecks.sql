CREATE TABLE [dbo].[CustomChecks] (
    [InstanceID]   INT            NOT NULL,
    [Test]         NVARCHAR (128) NOT NULL,
    [Context]      NVARCHAR (128) NOT NULL,
    [Status]       TINYINT        NOT NULL,
    [Info]         NVARCHAR (MAX) NULL,
    [SnapshotDate] DATETIME2 (3)  NOT NULL,
    CONSTRAINT [PK_DBACustomChecks] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [Test] ASC, [Context] ASC),
    CONSTRAINT [CK_CustomChecks_Status] CHECK ([Status]>=(1) AND [Status]<=(4)),
    CONSTRAINT [FK_DBACustomChecks_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

