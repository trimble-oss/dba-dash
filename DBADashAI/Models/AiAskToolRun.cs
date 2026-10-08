namespace DBADashAI.Models
{
    /// <summary>
    /// One tool that ran to answer a question, without its data.  Returned alongside the data so a
    /// follow-up can send back exactly what the first answer was built from - the row count and timing
    /// are part of that prompt, and a single-tool response carries neither anywhere else.
    /// </summary>
    public class AiAskToolRun
    {
        public string Tool { get; set; } = string.Empty;

        public int RowCount { get; set; }

        public long ExecutionMs { get; set; }
    }
}
