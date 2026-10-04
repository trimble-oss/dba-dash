using System;

namespace DBADashGUI.AI
{
    /// <summary>
    /// How long the GUI waits for the AI service to answer.
    ///
    /// One value for every caller, because the service has to give up before any of them does: a
    /// timeout it reports can say what happened, where a request the GUI abandons can only say it was
    /// cancelled.  The service's own provider timeouts (Ollama:TimeoutSeconds, default 240s) sit
    /// below this, with room left for the repository queries an Ask runs before the model is called.
    /// </summary>
    internal static class AIServiceTimeouts
    {
        internal static readonly TimeSpan Request = TimeSpan.FromSeconds(300);
    }
}
