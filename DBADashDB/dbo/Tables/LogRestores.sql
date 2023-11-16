CREATE TABLE dbo.LogRestores(
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    restore_date DATETIME2(3) NULL,
    backup_start_date DATETIME2(3) NULL,
    last_file NVARCHAR(260) NULL,
    backup_time_zone SMALLINT NULL,
    CONSTRAINT FK_LogRestores_Databases FOREIGN KEY (DatabaseID) REFERENCES dbo.Databases (DatabaseID),
    CONSTRAINT FK_LogRestores_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID),
    CONSTRAINT PK_LogRestores PRIMARY KEY CLUSTERED (InstanceID, DatabaseID)
);



