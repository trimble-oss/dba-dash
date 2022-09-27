CREATE TYPE dbo.IdentityColumns AS TABLE(
    database_id SMALLINT NOT NULL,
    object_id INT NOT NULL,
    object_name NVARCHAR(128)  NULL,
    column_name NVARCHAR(128) NULL,
    last_value BIGINT NULL,
    row_count BIGINT NULL,
    system_type_id TINYINT NOT NULL,
    user_type_id INT NOT NULL,
    max_length SMALLINT NOT NULL,
    increment_value BIGINT NULL,
    seed_value BIGINT NULL,
    schema_name NVARCHAR(128) NULL
);