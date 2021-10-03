CREATE TABLE dbo.MemoryClerkType(
	MemoryClerkTypeID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MemoryClerkType PRIMARY KEY,
	MemoryClerkType NVARCHAR(60) NOT NULL,
	MemoryClerkDescription VARCHAR(2000) NULL,
	INDEX IX_MemoryClerkType UNIQUE NONCLUSTERED(MemoryClerkType) 
)