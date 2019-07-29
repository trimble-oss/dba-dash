CREATE TABLE [dbo].[Drivers] (
    [InstanceID]         INT              NOT NULL,
    [ClassGuid]          UNIQUEIDENTIFIER NULL,
    [DeviceClass]        NVARCHAR (200)   NULL,
    [DeviceID]           NVARCHAR (200)   NOT NULL,
    [DeviceName]         NVARCHAR (200)   NULL,
    [DriverDate]         DATETIME         NULL,
    [DriverProviderName] NVARCHAR (200)   NULL,
    [DriverVersion]      NVARCHAR (200)   NULL,
    [FriendlyName]       NVARCHAR (200)   NULL,
    [HardWareID]         NVARCHAR (200)   NULL,
    [Manufacturer]       NVARCHAR (200)   NULL,
    [PDO]                NVARCHAR (200)   NULL,
    [ValidFrom]          DATETIME2 (2)    NOT NULL,
    CONSTRAINT [PK_Drivers] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DeviceID] ASC),
    CONSTRAINT [FK_Drivers_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);



