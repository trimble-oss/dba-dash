CREATE TABLE [dbo].[TraceFlags] (
    [InstanceID] INT           NOT NULL,
    [TraceFlag]  SMALLINT      NOT NULL,
    [ValidFrom]  DATETIME2 (2) NOT NULL,
    CONSTRAINT [PK_TraceFlags] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [TraceFlag] ASC),
    CONSTRAINT [FK_TraceFlags_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);



