CREATE TABLE [dbo].[TraceFlagHistory] (
    [InstanceID] INT           NOT NULL,
    [TraceFlag]  SMALLINT      NOT NULL,
    [ValidFrom]  DATETIME2 (2) NOT NULL,
    [ValidTo]    DATETIME2 (2) NOT NULL,
    CONSTRAINT [PK_TraceFlagHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [TraceFlag] ASC, [ValidTo] ASC)
);

