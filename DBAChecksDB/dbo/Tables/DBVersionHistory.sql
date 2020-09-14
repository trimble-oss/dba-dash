CREATE TABLE [dbo].[DBVersionHistory] (
    [DeployDate] DATETIME2 (3) NOT NULL,
    [Version]    VARCHAR (20)  NOT NULL,
    CONSTRAINT [PK_DBVersionHistory] PRIMARY KEY CLUSTERED ([DeployDate] ASC, [Version] ASC)
);


GO
EXECUTE sp_addextendedproperty @name = N'AppID', @value = N'74F9FD8A-DF22-4355-9A7A-6E1F4AE712B9', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'DBVersionHistory';

