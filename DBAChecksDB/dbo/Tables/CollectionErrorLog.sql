CREATE TABLE [dbo].[CollectionErrorLog] (
    [ErrorDate]    DATETIME2 (2)  NOT NULL,
    [InstanceID]   INT            NOT NULL,
    [ErrorSource]  VARCHAR (100)  NOT NULL,
    [ErrorMessage] NVARCHAR (MAX) NOT NULL,
    PRIMARY KEY CLUSTERED ([ErrorDate] ASC, [InstanceID] ASC, [ErrorSource] ASC),
    CONSTRAINT [FK_CollectionErrorLog_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);



