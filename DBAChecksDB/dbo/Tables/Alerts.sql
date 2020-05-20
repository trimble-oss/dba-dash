CREATE TABLE [dbo].[Alerts] (
    [InstanceID]                INT              NOT NULL,
    [id]                        INT              NOT NULL,
    [name]                      NVARCHAR (128)   NULL,
    [message_id]                INT              NULL,
    [severity]                  INT              NULL,
    [enabled]                   TINYINT          NULL,
    [delay_between_responses]   INT              NULL,
    [last_occurrence]           DATETIME2 (3)    NULL,
    [last_response]             DATETIME2 (3)    NULL,
    [notification_message]      NVARCHAR (512)   NULL,
    [include_event_description] TINYINT          NULL,
    [database_name]             NVARCHAR (512)   NULL,
    [event_description_keyword] NVARCHAR (100)   NULL,
    [occurrence_count]          INT              NULL,
    [count_reset]               DATETIME2 (3)    NULL,
    [job_id]                    UNIQUEIDENTIFIER NULL,
    [has_notification]          INT              NULL,
    [category_id]               INT              NULL,
    [performance_condition]     NVARCHAR (512)   NULL,
    CONSTRAINT [PK_Alerts] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [id] ASC),
    CONSTRAINT [FK_Alerts_Instance] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);



