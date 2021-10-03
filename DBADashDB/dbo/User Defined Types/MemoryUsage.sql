CREATE TYPE dbo.MemoryUsage AS TABLE(
    type NVARCHAR(60) PRIMARY KEY,
    pages_kb BIGINT NOT NULL,
    virtual_memory_reserved_kb BIGINT NOT NULL,
    virtual_memory_committed_kb BIGINT NOT NULL,
    awe_allocated_kb BIGINT NOT NULL,
    shared_memory_reserved_kb BIGINT NOT NULL,
    shared_memory_committed_kb BIGINT NOT NULL
);
