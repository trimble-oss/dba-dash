#nullable enable
using DBADash;
using DBADashGUI.AI;
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
    ///
    /// A conversation comes from two places.  Its opening analysis is shared and lives in the
    /// repository; the reader's own follow-ups are private and live on their machine
    /// (<see cref="AiLocalConversationStore"/>).  They are put back together here, by conversation id,
    /// so what reaches the viewer is one exchange rather than two halves - and so a reader with no
    /// repository to reach still sees their own.
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
            if (string.IsNullOrWhiteSpace(signature)) return Array.Empty<Entry>();

            var shared = await FetchSharedAsync(signature, planHash, cancellationToken);
            var local = await AiLocalConversationStore.ForPlanAsync(signature, planHash, cancellationToken);

            return Merge(shared, local, planHash);
        }

        /// <summary>
        /// The conversations the whole team can see: one opening analysis each, plus any follow-ups
        /// stored back when follow-ups went to the repository.
        /// </summary>
        private static async Task<List<Entry>> FetchSharedAsync(
            string signature,
            string? planHash,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Common.ConnectionString)) return new List<Entry>();

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
                // the same, it just cannot offer what was found before.  The reader's own conversations
                // are unaffected - they are not in there.
                Log.Debug(ex, "Could not read query plan analysis history for {signature}", signature);
                return new List<Entry>();
            }
        }

        /// <summary>
        /// Puts the two halves of each conversation back together: the shared opening analysis from the
        /// repository, then the reader's own follow-ups from this machine.
        ///
        /// A local conversation whose opener is missing is still shown, as itself.  That happens when
        /// the analysis was never recorded - the service's write is best effort - and the reader's half
        /// of an exchange is worth more than the tidiness of hiding it.
        /// </summary>
        private static IReadOnlyList<Entry> Merge(
            List<Entry> shared,
            IReadOnlyList<AiLocalConversationStore.StoredConversation> local,
            string? planHash)
        {
            var merged = new List<Entry>(shared);

            var index = new Dictionary<Guid, int>();
            for (var i = 0; i < merged.Count; i++)
            {
                if (merged[i].ConversationId is { } id) index[id] = i;
            }

            foreach (var conversation in local)
            {
                var turns = conversation.Turns
                    .Select(t => new Turn(
                        0, // No repository row, so no id - nothing reads it.
                        t.Question,
                        t.Answer,
                        t.Model ?? string.Empty,
                        conversation.PayloadVersion ?? string.Empty,
                        t.GeneratedUtc))
                    .ToList();

                if (index.TryGetValue(conversation.ConversationId, out var at))
                {
                    merged[at] = merged[at] with { Turns = merged[at].Turns.Concat(turns).ToList() };
                }
                else
                {
                    merged.Add(new Entry(
                        "local:" + conversation.ConversationId,
                        conversation.ConversationId,
                        turns,
                        conversation.Instance,
                        string.Equals(conversation.ArtifactHash, planHash, StringComparison.OrdinalIgnoreCase)));
                }
            }

            // The procedure ordered what it returned; a local conversation has to be placed among it,
            // so the whole list is ordered again on the same two keys.
            return merged
                .Where(c => c.Turns.Count > 0)
                .OrderByDescending(c => c.IsThisPlan)
                .ThenByDescending(c => c.GeneratedUtc)
                .ToList();
        }
    }
}
