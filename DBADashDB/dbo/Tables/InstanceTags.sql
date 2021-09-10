CREATE TABLE [dbo].[InstanceTags] (
    [Instance] [sysname] NOT NULL,
    [TagID]    INT  NOT NULL,
    CONSTRAINT [PK_InstanceTags] PRIMARY KEY CLUSTERED ([Instance] ASC, [TagID] ASC),
    CONSTRAINT [FK_InstanceTags_Tags] FOREIGN KEY ([TagID]) REFERENCES [dbo].[Tags] ([TagID])
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [InstanceTags_TagID]
    ON [dbo].[InstanceTags]([TagID] ASC, [Instance] ASC);

