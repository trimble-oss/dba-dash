CREATE TABLE dbo.Spinlock (
    SpinlockID SMALLINT IDENTITY (1, 1) NOT NULL,
    SpinlockName NVARCHAR (256) NOT NULL,
    Description NVARCHAR (2000) NULL,
    CONSTRAINT PK_Spinlock PRIMARY KEY CLUSTERED (SpinlockID ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Spinlock_SpinlockName
    ON dbo.Spinlock(SpinlockName ASC);
