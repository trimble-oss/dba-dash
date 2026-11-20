using Serilog.Context;
using System;

namespace DBADash
{
    /// <summary>
    /// Adds a property to everything logged while a piece of work runs - including what's logged by code that doesn't
    /// know what the work is for.  In the JSON log each is a field on every event, so the log can be filtered to one
    /// connection, or one message from the GUI.  Needs Enrich.FromLogContext.  Until disposed, events logged on the
    /// same async flow carry the property; a message template's own property of the same name wins over it.
    /// </summary>
    public static class LogScope
    {
        /// <summary>The property names, as message templates use them - see LogTemplateConventionTests.</summary>
        public const string ConnectionIDProperty = "ConnectionID";
        public const string MessageIdProperty = "MessageId";
        public const string HandleProperty = "Handle";

        /// <summary>Work for one source connection: a collection, an import, a message from the GUI.  Null when there's no ID to add.</summary>
        public static IDisposable Connection(string connectionID) =>
            string.IsNullOrEmpty(connectionID) ? null : LogContext.PushProperty(ConnectionIDProperty, connectionID);

        /// <summary>A message from the GUI, by its own ID - the one a cancellation names.</summary>
        public static IDisposable Message(Guid messageId) =>
            messageId == Guid.Empty ? null : LogContext.PushProperty(MessageIdProperty, messageId);

        /// <summary>The conversation a message from the GUI arrived on, and that its replies go back on.</summary>
        public static IDisposable Handle(Guid handle) =>
            handle == Guid.Empty ? null : LogContext.PushProperty(HandleProperty, handle);
    }
}
