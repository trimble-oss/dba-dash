﻿CREATE TABLE [dbo].[CollectionErrorLog] (
    [ErrorDate]    DATETIME2 (2)  NOT NULL,
    [InstanceID]   INT            NULL,
    [ErrorSource]  VARCHAR (100)  NOT NULL,
    [ErrorMessage] NVARCHAR (MAX) NOT NULL,
    [ErrorContext] VARCHAR (100)  NULL,
    [ErrorID]      INT            IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_CollectionErrorLog] PRIMARY KEY NONCLUSTERED ([ErrorID] ASC),
    CONSTRAINT [FK_CollectionErrorLog_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

GO
CREATE CLUSTERED INDEX [IX_CollectionErrorLog_ErrorDate]
    ON [dbo].[CollectionErrorLog]([ErrorDate] ASC);
GO
CREATE NONCLUSTERED INDEX IX_CollectionErrorLog_InstanceID_ErrorDate ON dbo.CollectionErrorLog(InstanceID,ErrorDate) INCLUDE(ErrorSource,ErrorContext)
