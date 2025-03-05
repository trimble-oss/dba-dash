﻿CREATE TABLE dbo.ErrorMessage(
	ErrorMessageID BIGINT NOT NULL IDENTITY(1,1) CONSTRAINT PK_ErrorMessage PRIMARY KEY,
	Message NVARCHAR(500) NOT NULL,
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_ErrorMessage_Message ON dbo.ErrorMessage(Message)