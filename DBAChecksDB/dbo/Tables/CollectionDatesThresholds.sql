CREATE TABLE [dbo].[CollectionDatesThresholds] (
    [InstanceID]        INT          NOT NULL,
    [Reference]         VARCHAR (30) NOT NULL,
    [WarningThreshold]  INT          NULL,
    [CriticalThreshold] INT          NULL,
    CONSTRAINT [PK_CollectionDatesThresholds] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [Reference] ASC)
);

