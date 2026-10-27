CREATE TYPE dbo.ServiceBrokerTransmissionQueue AS TABLE (
    database_name NVARCHAR(128) NOT NULL,
    to_service_name NVARCHAR(256) NULL,
    transmission_status NVARCHAR(4000) NULL,
    message_count BIGINT NOT NULL,
    oldest_enqueue_time DATETIME NULL
);
