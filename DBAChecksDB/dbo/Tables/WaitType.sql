CREATE TABLE [dbo].[WaitType] (
    [WaitTypeID]     SMALLINT      IDENTITY (1, 1) NOT NULL,
    [WaitType]       NVARCHAR (60) NOT NULL,
    [IsCriticalWait] BIT           CONSTRAINT [DF_WaitTyppe_IsCriticalWait] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_WaitType] PRIMARY KEY CLUSTERED ([WaitTypeID] ASC)
);




GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_WaitType_WaitType]
    ON [dbo].[WaitType]([WaitType] ASC);

