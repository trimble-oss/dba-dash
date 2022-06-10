CREATE TABLE dbo.Counters (
    CounterID     INT            IDENTITY (1, 1) NOT NULL,
    object_name   NVARCHAR (128) NOT NULL,
    counter_name  NVARCHAR (128) NOT NULL,
    instance_name NVARCHAR (128) NOT NULL,
	CriticalFrom DECIMAL (28, 9) NULL,
    CriticalTo DECIMAL (28, 9)  NULL,
	WarningFrom DECIMAL (28, 9) NULL,
	WarningTo DECIMAL (28, 9)  NULL,
	GoodFrom DECIMAL (28, 9)  NULL,
	GoodTo DECIMAL (28, 9)  NULL,
    SystemCriticalFrom DECIMAL (28, 9) NULL,
    SystemCriticalTo DECIMAL (28, 9)  NULL,
	SystemWarningFrom DECIMAL (28, 9) NULL,
	SystemWarningTo DECIMAL (28, 9)  NULL,
	SystemGoodFrom DECIMAL (28, 9)  NULL,
	SystemGoodTo DECIMAL (28, 9)  NULL,
    CONSTRAINT [PK_Counters] PRIMARY KEY CLUSTERED (CounterID ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Counters_object_name_counter_name_instance_name
    ON dbo.Counters(object_name ASC, counter_name ASC, instance_name ASC);

