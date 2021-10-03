CREATE PROC dbo.MemoryConfig_Get(
	@InstanceID INT
)
AS
SELECT uPvt.Config, 
		uPvt.Value
FROM (
		SELECT CAST(FORMAT(PhysicalMemoryGB,'N1') AS NVARCHAR(128)) AS [Physical Memory (GB)],
					CAST(FORMAT(BufferPoolMB/1024.0,'N1') AS NVARCHAR(128)) AS [Max Server Memory (GB)],
					CAST(FORMAT(PctMemoryAllocatedToBufferPool,'P1') AS NVARCHAR(128)) AS [Max Server Memory %],
					CAST(FORMAT(MemoryNotAllocatedToBufferPoolGB,'N1') AS NVARCHAR(128)) AS [Memory NOT allocated to Max Server Memory (GB)],
					CAST(sql_memory_model_desc AS NVARCHAR(128)) AS [Memory Model]
		FROM dbo.InstanceInfo
		WHERE InstanceID=@InstanceID
	) I
UNPIVOT (Value FOR Config IN([Physical Memory (GB)],
							[Max Server Memory (GB)],
							[Max Server Memory %],
							[Memory Model],
							[Memory NOT allocated to Max Server Memory (GB)]
							)
		) AS uPvt