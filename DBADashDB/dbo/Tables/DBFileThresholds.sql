CREATE TABLE [dbo].[DBFileThresholds] (
    [InstanceID]                  INT            NOT NULL,
    [DatabaseID]                  INT            NOT NULL,
    [data_space_id]               INT            NOT NULL,
    [FreeSpaceWarningThreshold]   DECIMAL (9, 3) NULL,
    [FreeSpaceCriticalThreshold]  DECIMAL (9, 3) NULL,
    [FreeSpaceCheckType]          CHAR (1)       NOT NULL,
    [PctMaxSizeWarningThreshold]  DECIMAL (9, 3) NULL,
    [PctMaxSizeCriticalThreshold] DECIMAL (9, 3) NULL,
    CONSTRAINT [PK_DBFileThresholds] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DatabaseID] ASC, [data_space_id] ASC)
);



