CREATE TABLE dbo.MemoryUsage(
	InstanceID INT NOT NULL,
	SnapshotDate DATETIME2(2),
	MemoryClerkTypeID INT NOT NULL,
    pages_kb BIGINT NOT NULL,
    virtual_memory_reserved_kb BIGINT NOT NULL,
    virtual_memory_committed_kb BIGINT NOT NULL,
    awe_allocated_kb BIGINT NOT NULL,
    shared_memory_reserved_kb BIGINT NOT NULL,
    shared_memory_committed_kb BIGINT NOT NULL,
	CONSTRAINT FK_MemoryUsage_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
    CONSTRAINT FK_MemoryUsage_MemoryClerkType FOREIGN KEY(MemoryClerkTypeID) REFERENCES dbo.MemoryClerkType(MemoryClerkTypeID),
	CONSTRAINT PK_MemoryUsage PRIMARY KEY(InstanceID,SnapshotDate,MemoryClerkTypeID) WITH (DATA_COMPRESSION = PAGE) ON PS_MemoryUsage(SnapshotDate)
) ON PS_MemoryUsage(SnapshotDate)
