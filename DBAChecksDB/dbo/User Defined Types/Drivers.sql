CREATE TYPE [dbo].[Drivers] AS TABLE (
    [ClassGuid]          UNIQUEIDENTIFIER NULL,
    [DeviceClass]        NVARCHAR (200)   NULL,
    [DeviceID]           NVARCHAR (200)   NULL,
    [DeviceName]         NVARCHAR (200)   NULL,
    [DriverDate]         DATETIME         NULL,
    [DriverProviderName] NVARCHAR (200)   NULL,
    [DriverVersion]      NVARCHAR (200)   NULL,
    [FriendlyName]       NVARCHAR (200)   NULL,
    [HardWareID]         NVARCHAR (200)   NULL,
    [Manufacturer]       NVARCHAR (200)   NULL,
    [PDO]                NVARCHAR (200)   NULL);



