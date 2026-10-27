CREATE TYPE dbo.ServiceBrokerConversations AS TABLE (
    database_name NVARCHAR(128) NOT NULL,
    state CHAR(2) NOT NULL,
    conversation_count BIGINT NOT NULL
);
