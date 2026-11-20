using Serilog;
using Serilog.Events;
using SerilogTimings;

namespace DBADash
{
    /// <summary>
    /// SerilogTimings operations at a level other than its default - use this rather than <c>Operation.At(level)</c>.
    /// <para>
    /// When an <c>Operation.At(level)</c> operation's levels aren't enabled - Debug, with the log at Information -
    /// SerilogTimings returns one shared Operation for every call.  An Operation pushes its OperationId onto the log
    /// context and keeps the handle that restores the context as it was; shared, concurrent callers overwrite each
    /// other's handle, so completing one restores another flow's context.  What's logged after it then carries that
    /// flow's properties - its ConnectionID among them.  Here, an operation whose level is off isn't started at all.
    /// </para>
    /// </summary>
    public static class LogTiming
    {
        /// <summary>
        /// Begins an operation logged at <paramref name="level"/> - or returns null when that level isn't enabled, so
        /// callers complete it with <c>op?.Complete()</c>.  A <c>using</c> of null is fine.
        /// </summary>
        public static Operation Begin(LogEventLevel level, string messageTemplate, params object[] args) =>
            Log.IsEnabled(level) ? Operation.At(level).Begin(messageTemplate, args) : null;
    }
}
