namespace DBADash.Deadlock.Analysis
{
    /// <summary>How much a finding is asking of the reader.</summary>
    public enum DeadlockFindingSeverity
    {
        /// <summary>Something the graph says that is worth knowing, with nothing to act on.</summary>
        Information,

        /// <summary>A pattern with a known remedy - the findings people are here for.</summary>
        Advice,

        /// <summary>Something that undermines the graph itself, such as a truncated capture.</summary>
        Warning
    }
}
