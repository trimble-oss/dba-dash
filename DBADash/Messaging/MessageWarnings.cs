using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADash.Messaging
{
    /// <summary>
    /// Warnings returned with a successful reply: the request was done, but something the user should know about didn't
    /// work - e.g. a pause was applied, but the repository couldn't be told, so alerts aren't muted.  Carried as a table
    /// in the reply's DataSet, which every message can already return.
    /// </summary>
    public static class MessageWarnings
    {
        public const string TableName = "Warnings";

        /// <summary>A DataSet holding <paramref name="warnings"/>, or null when there are none.</summary>
        public static DataSet ToDataSet(IReadOnlyCollection<string> warnings)
        {
            if (warnings is not { Count: > 0 }) return null;
            var dt = new DataTable(TableName);
            dt.Columns.Add("Message", typeof(string));
            foreach (var warning in warnings)
            {
                dt.Rows.Add(warning);
            }
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>The warnings in a reply's DataSet - empty when there are none.</summary>
        public static List<string> Read(DataSet ds) =>
            ds?.Tables[TableName]?.Rows.Cast<DataRow>().Select(r => (string)r["Message"]).ToList() ?? new List<string>();

        /// <summary>
        /// The warning for schedule info that couldn't be reported: the change was made on the service, but the repository
        /// won't reflect it (<paramref name="consequence"/>) until it's reported again.  Null when nothing failed.
        /// </summary>
        public static string ForScheduleInfoFailures(IReadOnlyList<string> failures, string consequence) =>
            failures is not { Count: > 0 }
                ? null
                : $"The repository couldn't be updated for {failures.Count} instance(s), so {consequence} until the service reports it again (after the next change, or when the service starts).  {failures[0]}";
    }
}
