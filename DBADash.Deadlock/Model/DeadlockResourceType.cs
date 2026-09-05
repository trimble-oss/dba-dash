namespace DBADash.Deadlock.Model
{
    /// <summary>
    /// The kind of resource involved in a deadlock, mapped from the element name used in the
    /// resource-list of the deadlock graph.  Anything not recognised maps to <see cref="Unknown"/>,
    /// where <see cref="DeadlockResource.TypeName"/> still carries the raw element name and
    /// <see cref="DeadlockResource.Attributes"/> carries its attributes, so an unrecognised
    /// resource can still be displayed in full.
    /// </summary>
    public enum DeadlockResourceType
    {
        Unknown = 0,
        KeyLock,
        PageLock,
        ObjectLock,
        RidLock,
        HobtLock,
        AllocUnitLock,
        DatabaseLock,
        FileLock,
        ExtentLock,
        ApplicationLock,
        MetadataLock,
        TransactionLock,

        /// <summary>Parallel query exchange (intra-query) deadlock.</summary>
        ExchangeEvent,

        /// <summary>Waiting on a worker thread from the thread pool.</summary>
        ThreadPoolWait,

        /// <summary>WAITFOR.</summary>
        WaitFor,

        /// <summary>Parallel query synchronisation point.</summary>
        SyncPoint
    }
}
