CREATE TABLE [dbo].[WaitType] (
    [WaitTypeID] SMALLINT      IDENTITY (1, 1) NOT NULL,
    [WaitType]   NVARCHAR (60) NOT NULL,
    CONSTRAINT [PK_WaitType] PRIMARY KEY CLUSTERED ([WaitTypeID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_WaitType_WaitType]
    ON [dbo].[WaitType]([WaitType] ASC);

