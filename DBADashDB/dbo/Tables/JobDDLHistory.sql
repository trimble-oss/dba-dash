CREATE TABLE [dbo].[JobDDLHistory] (
    [InstanceID]     INT              NOT NULL,
    [job_id]         UNIQUEIDENTIFIER NOT NULL,
    [version_number] INT              NOT NULL,
    [SnapshotDate]   DATETIME2 (2)    NOT NULL,
    [date_modified]  DATETIME2 (2)    NOT NULL,
    [DDLID]          BIGINT           NOT NULL,
    CONSTRAINT [PK_JobDDLHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [job_id] ASC, [SnapshotDate] ASC),
    CONSTRAINT [FK_JobDDLHistory_DDL] FOREIGN KEY ([DDLID]) REFERENCES [dbo].[DDL] ([DDLID]),
    CONSTRAINT [FK_JobDDLHistory_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

