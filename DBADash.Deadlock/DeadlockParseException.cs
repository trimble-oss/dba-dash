using System;

namespace DBADash.Deadlock
{
    /// <summary>
    /// Thrown when a string cannot be interpreted as a SQL Server deadlock graph, either because it
    /// is not well formed XML or because it contains no deadlock element.
    /// </summary>
    public sealed class DeadlockParseException : Exception
    {
        public DeadlockParseException(string message) : base(message)
        {
        }

        public DeadlockParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
