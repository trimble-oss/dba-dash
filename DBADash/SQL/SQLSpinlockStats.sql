SELECT name AS spinlock_name,
       collisions,
       spins,
       CAST(sleep_time AS BIGINT) AS sleep_time,
       CAST(backoffs AS BIGINT) AS backoffs
FROM sys.dm_os_spinlock_stats WITH (NOLOCK)
WHERE collisions > 0
OR backoffs > 0;
