CREATE TABLE [dbo].[CounterMapping] (
    [object_name]       NVARCHAR (128) NOT NULL,
    [counter_name]      NVARCHAR (128) NOT NULL,
    [base_counter_name] NVARCHAR (128) NOT NULL,
    CONSTRAINT [PK_CounterMapping] PRIMARY KEY CLUSTERED ([object_name] ASC, [counter_name] ASC)
);

