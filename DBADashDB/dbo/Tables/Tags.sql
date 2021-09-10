CREATE TABLE [dbo].[Tags] (
    [TagID]    INT      IDENTITY (1, 1) NOT NULL,
    [TagName]  NVARCHAR (50) NULL,
    [TagValue] NVARCHAR (128) NULL,
    CONSTRAINT [PK_Tags] PRIMARY KEY CLUSTERED ([TagID] ASC)
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Tags_TagName_TagValue]
    ON [dbo].[Tags]([TagName] ASC, [TagValue] ASC);

