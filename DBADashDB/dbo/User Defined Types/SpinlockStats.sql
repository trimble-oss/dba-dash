CREATE TYPE dbo.SpinlockStats AS TABLE (
    spinlock_name NVARCHAR (256) NOT NULL,
    collisions BIGINT NOT NULL,
    spins BIGINT NOT NULL,
    sleep_time BIGINT NOT NULL,
    backoffs BIGINT NOT NULL
);
