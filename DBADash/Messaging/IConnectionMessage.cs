namespace DBADash.Messaging
{
    /// <summary>A message for one source connection - its ConnectionID is added to what's logged while it's processed.  See <see cref="LogScope"/>.</summary>
    public interface IConnectionMessage
    {
        string ConnectionID { get; }
    }
}
