CREATE TABLE [dbo].[ObjectType] (
    [ObjectType]      CHAR (3)     NOT NULL,
    [TypeDescription] VARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([ObjectType] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ObjectType_TypeDescription]
    ON [dbo].[ObjectType]([TypeDescription] ASC);

