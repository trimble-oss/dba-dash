CREATE TYPE [dbo].[CollectionError] AS TABLE (
    [ErrorSource]  VARCHAR (100)  NOT NULL,
    [ErrorMessage] NVARCHAR (MAX) NOT NULL,
    [ErrorContext] VARCHAR (100)  NULL);



