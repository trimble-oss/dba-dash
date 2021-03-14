SELECT configuration_id,CAST(value as BIGINT) as value,CAST(value_in_use as BIGINT) as value_in_use
FROM sys.configurations