CREATE TYPE dbo.LogRestores AS TABLE(
    database_name NVARCHAR(128) NULL,
    restore_date DATETIME NULL,
    backup_start_date DATETIME NULL,
    last_file NVARCHAR(260) NULL,
    backup_time_zone SMALLINT NULL
);

