CREATE TABLE [dbo].[CollectionDatesThresholds] (
    [InstanceID]        INT             NOT NULL,
    [Reference]         VARCHAR (30)    NOT NULL,
    [WarningThreshold]  INT             NULL,
    [CriticalThreshold] INT             NULL,
    [WarningMultiplier]     DECIMAL (9, 2)  NULL,
    [CriticalMultiplier]    DECIMAL (9, 2)  NULL,
    [WarningBufferMinutes]  DECIMAL (9, 2)  NULL,
    [CriticalBufferMinutes] DECIMAL (9, 2)  NULL,
    [Disabled]              BIT             NOT NULL CONSTRAINT DF_CollectionDatesThresholds_Disabled DEFAULT (0),
    CONSTRAINT [PK_CollectionDatesThresholds] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [Reference] ASC)
);

