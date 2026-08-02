CREATE TABLE dbo.AgentJobExclusions(
    AgentJobExclusionID INT IDENTITY(1,1) NOT NULL,
    InstanceID INT NOT NULL, /* -1 = Root (applies to all instances) */
    JobNameFilter NVARCHAR(128) NULL, /* LIKE-or-equals match against the job name.  NULL = match any job name */
    CategoryFilter NVARCHAR(128) NULL, /* LIKE-or-equals match against the job category.  NULL = match any category */
    DescriptionFilter NVARCHAR(512) NULL, /* LIKE-or-equals match against the job description.  NULL = match any description */
    CONSTRAINT PK_AgentJobExclusions PRIMARY KEY CLUSTERED (AgentJobExclusionID),
    /* At least one filter must be supplied - an exclusion with no filters would exclude every job */
    CONSTRAINT CK_AgentJobExclusions_AtLeastOneFilter CHECK (JobNameFilter IS NOT NULL OR CategoryFilter IS NOT NULL OR DescriptionFilter IS NOT NULL)
);
GO
CREATE NONCLUSTERED INDEX IX_AgentJobExclusions_InstanceID
    ON dbo.AgentJobExclusions(InstanceID);
