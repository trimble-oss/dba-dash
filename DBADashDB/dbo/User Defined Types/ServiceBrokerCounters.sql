CREATE TYPE dbo.ServiceBrokerCounters AS TABLE (
    instance_name NVARCHAR(128) NOT NULL,
    counter_name NVARCHAR(128) NOT NULL,
    cntr_value BIGINT NOT NULL
);
