CREATE TYPE dbo.ServiceBrokerEndpoint AS TABLE (
    endpoint_name NVARCHAR(128) NOT NULL,
    state_desc NVARCHAR(60) NULL,
    port INT NULL,
    connection_count INT NULL,
    connected_count INT NULL,
    login_failed_count INT NULL,
    last_activity_time DATETIME NULL
);
