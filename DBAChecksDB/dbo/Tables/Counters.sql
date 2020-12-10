CREATE TABLE [dbo].[Counters] (
    [CounterID]     INT            IDENTITY (1, 1) NOT NULL,
    [object_name]   NVARCHAR (128) NOT NULL,
    [counter_name]  NVARCHAR (128) NOT NULL,
    [instance_name] NVARCHAR (128) NOT NULL,
    CONSTRAINT [PK_Counters] PRIMARY KEY CLUSTERED ([CounterID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Counters_object_name_counter_name_instance_name]
    ON [dbo].[Counters]([object_name] ASC, [counter_name] ASC, [instance_name] ASC);

