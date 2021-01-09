CREATE TYPE [dbo].[CustomChecks] AS TABLE (
    [Test]    NVARCHAR (128) NOT NULL,
    [Context] NVARCHAR (128) NOT NULL,
    [Status]  TINYINT        NOT NULL,
    [Info]    NVARCHAR (MAX) NULL);

