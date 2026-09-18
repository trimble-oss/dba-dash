using System;

namespace DBADash.QueryPlan
{
    /// <summary>
    /// The input was not a query plan we could read.
    ///
    /// Its own exception type so a caller can tell "this file is not a plan" - worth a clear message
    /// to the reader - from a bug in the parser, which is not.
    /// </summary>
    public sealed class PlanParseException : Exception
    {
        public PlanParseException(string message) : base(message)
        {
        }

        public PlanParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
