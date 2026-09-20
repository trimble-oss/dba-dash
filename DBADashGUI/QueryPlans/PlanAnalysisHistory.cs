#nullable enable
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// Previous AI conversations about a query plan, read straight from the repository: those about this
    /// exact plan first, then those about the same query with a different plan, newest first within each.
    ///
    /// The query plan counterpart of <see cref="Deadlocks.DeadlockAnalysisHistory"/>, and the same in
    /// every respect but one: leading with the plan matters more here.  A query that has had two plans
    /// has usually had a good one and a bad one, so an answer is labelled with which of the two it is
    /// about rather than quietly offered as an answer about what is on screen.
    ///
    /// Reading the repository directly rather than going through the AI service means a plan's history
    /// is there even when no AI service is configured any more.
    /// </summary>
    internal static class PlanAnalysisHistory
    {
        /// <summary>One answer, with the follow-up question that produced it where there was one.</summary>
        internal sealed record Turn(
            long Id,
            string? Question,
            string Analysis,
            string Model,
            string PayloadVersion,
            DateTime GeneratedUtc);

        internal sealed record Entry(
            string Key,
            Guid? ConversationId,
            IReadOnlyList<Turn> Turns,
            string? Instance,
            bool IsThisPlan)
        {
            /// <summary>The model that answered last, which is the one still answering if this is carried on.</summary>
            public string Model => Turns[^1].Model;

            /// <summary>When the conversation last had something added to it.</summary>
            public DateTime GeneratedUtc => Turns[^1].GeneratedUtc;

            /// <summary>
            /// True when the conversation was started without the plan XML.  Taken from the opening turn,
            /// which is the one that carried the artifact.
            /// </summary>
            public bool WithoutPlanXml => !Turns[0].PayloadVersion.Contains("xml", StringComparison.OrdinalIgnoreCase);

            /// <summary>Whether the conversation is about this plan or about another plan for the same query.</summary>
            public string Scope => IsThisPlan ? "this plan" : "this query, with a different plan";

            /// <summary>How the conversation reads in a menu: one answer, or an exchange of several.</summary>
            public string Length => Turns.Count == 1 ? "1 answer" : $"{Turns.Count} answers";
        }

        internal static async Task<IReadOnlyList<Entry>> FetchAsync(
            string? signature,
            string? planHash,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(Common.ConnectionString))
            {
                return Array.Empty<Entry>();
            }

            try
            {
                // Built up as the rows arrive, which come conversation by conversation and in turn order
                // within each - so the first row of a conversation is the one that describes it.
                var conversations = new List<Entry>();
                var turns = new Dictionary<string, List<Turn>>();

                await using var connection = new SqlConnection(Common.ConnectionString);
                await using var command = new SqlCommand("AI.QueryPlanAnalysisHistory_Get", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 30
                };

                command.Parameters.AddWithValue("Signature", signature);
                command.Parameters.AddWithValue("PlanHash", (object?)planHash ?? DBNull.Value);

                await connection.OpenAsync(cancellationToken);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var key = (string)reader["ConversationKey"];

                    if (!turns.TryGetValue(key, out var list))
                    {
                        list = new List<Turn>();
                        turns[key] = list;
                        conversations.Add(new Entry(
                            key,
                            reader["ConversationID"] as Guid?,
                            list,
                            reader["InstanceDisplayName"] as string,
                            (bool)reader["IsThisPlan"]));
                    }

                    list.Add(new Turn(
                        (long)reader["QueryPlanAnalysisID"],
                        reader["Question"] as string,
                        (string)reader["Analysis"],
                        (string)reader["Model"],
                        (string)reader["PayloadVersion"],
                        (DateTime)reader["GeneratedUtc"]));
                }

                // A conversation with no turns cannot happen - the row is what creates it - but an empty
                // one would make Entry's summaries throw, and nothing here is worth an exception.
                return conversations.Where(c => c.Turns.Count > 0).ToList();
            }
            catch (Exception ex)
            {
                // An older repository without the table, or a user without the grant: the viewer works
                // the same, it just cannot offer what was found before.
                Log.Debug(ex, "Could not read query plan analysis history for {signature}", signature);
                return Array.Empty<Entry>();
            }
        }
    }
}
