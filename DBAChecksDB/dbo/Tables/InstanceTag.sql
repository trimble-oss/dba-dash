CREATE TABLE [dbo].[InstanceTag] (
    [InstanceID] INT      NOT NULL,
    [TagID]      SMALLINT NOT NULL,
    CONSTRAINT [PK_InstanceTag] PRIMARY KEY CLUSTERED ([TagID] ASC, [InstanceID] ASC),
    CONSTRAINT [FK_InstanceTags] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID]),
    CONSTRAINT [FK_Tags] FOREIGN KEY ([TagID]) REFERENCES [dbo].[Tags] ([TagID])
);

