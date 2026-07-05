CREATE TYPE [dbo].[ScheduleInfo] AS TABLE (
    [Reference]               VARCHAR (100)   NOT NULL,
    [Schedule]                VARCHAR (100)   NOT NULL,
    [RunOnServiceStart]       BIT             NOT NULL,
    [MaxIntervalMinutes]      DECIMAL (18, 2) NULL,
    [IsInstanceOverride]      BIT             NOT NULL
);
