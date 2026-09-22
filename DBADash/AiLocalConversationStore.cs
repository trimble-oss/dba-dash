#nullable enable
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash
{
    /// <summary>
    /// The reader's own follow-up conversations, kept on their machine rather than in the repository.
    ///
    /// The split is about who the content belongs to.  An opening analysis is a plan or a graph and
    /// some generated notes submitted for analysis: nothing in it belongs to whoever pressed the
    /// button, everyone benefits from finding it next time the query turns up, and it goes in the
    /// repository where the whole team can read it.  What follows is the reader thinking out loud -
    /// what they were chasing, what their schema looks like, what they were about to try - and that is
    /// theirs.  It never leaves their profile.
    ///
    /// Local rather than encrypted-in-the-repository, and deliberately.  Storing it centrally means
    /// custom cryptography, key custody, and a shared table whose contents are only as private as the
    /// next migration leaves them.  Storing it here means DPAPI - the operating system's own mechanism,
    /// no key for anyone to manage or lose - and a blast radius of one workstation.  "Can an
    /// administrator read my conversation" stops being a question about key custody and becomes a
    /// question about whether they have your computer.
    ///
    /// The cost is honest and worth stating: these conversations do not roam, and a rebuilt machine
    /// loses them even though the repository is backed up perfectly.  Export is the answer to both,
    /// and it is a deliberate act rather than something that happens quietly in the background.
    ///
    /// One file per conversation, rather than one file rewritten whole, and the reason is that this
    /// application runs more than once at a time.  The single-instance mutex covers only the standalone
    /// file viewer (see Program.RunViewers), so a reader can have the main window open all day and
    /// double-click a plan in Explorer, which starts a second process with its own copy of this.  Two
    /// processes each holding the whole store in memory and writing it back whole means the second one
    /// to save silently discards whatever the first added.  Appending to one small file cannot do that,
    /// because they are appending to different files.
    ///
    /// It buys two more things worth having.  A file that will not open costs one conversation instead
    /// of all of them, and conversations become individually prunable rather than an ever-growing blob
    /// with no natural place to cut.
    /// </summary>
    public static class AiLocalConversationStore
    {
        /// <summary>Which viewer a conversation belongs to.  The two never match each other's records.</summary>
        public enum Artifact
        {
            QueryPlan,
            Deadlock
        }

        public sealed record StoredTurn(string? Question, string Answer, string? Model, DateTime GeneratedUtc);

        /// <summary>
        /// One conversation's follow-ups.  Not the whole conversation: turn 1 is the shared opening
        /// analysis and lives in the repository, and <see cref="ConversationId"/> is what puts the two
        /// halves back together.
        /// </summary>
        public sealed record StoredConversation
        {
            public Guid ConversationId { get; init; }

            public Artifact Kind { get; init; }

            /// <summary>The query hash, or the deadlock's pattern signature, as the viewer had it.</summary>
            public string? Signature { get; init; }

            /// <summary>
            /// The plan hash, or the deadlock's occurrence hash.
            ///
            /// This is the identity to trust for deadlocks.  A pattern signature is recomputed when the
            /// signature version changes - the repository moves its own rows, and nothing moves these -
            /// so a conversation found only by signature can drift out of its pattern.  The occurrence
            /// hash is computed from the graph and never changes, so a conversation about this exact
            /// deadlock is always found.  For plans both come from SQL Server and are stable.
            /// </summary>
            public string? ArtifactHash { get; init; }

            public string? Instance { get; init; }

            /// <summary>What the query was, so a conversation can be described rather than listed as a hash.</summary>
            public string? StatementText { get; init; }

            public string? PayloadVersion { get; init; }

            public DateTime UpdatedUtc { get; set; }

            public List<StoredTurn> Turns { get; init; } = new();
        }

        private static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Where the store lives.  Overridden only by tests, which must not write to - or read - the
        /// profile of whoever is running them.
        /// </summary>
        internal static string? FolderOverride;

        private static string Folder => FolderOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash");

        /// <summary>One file per conversation, named for its id.</summary>
        private static string ConversationFolder => Path.Combine(Folder, "AiConversations");

        private static string PathFor(Guid conversationId) =>
            Path.Combine(ConversationFolder, conversationId.ToString("D") + ".dat");

        /// <summary>
        /// What has been read so far, by conversation.  Files are read once and kept; the directory is
        /// rescanned on every read so that a conversation another process has since written turns up
        /// without waiting for a restart.
        /// </summary>
        private static readonly Dictionary<Guid, StoredConversation> Cached = new();

        /// <summary>
        /// Files that were there and would not open.  Remembered so they are not retried on every scan,
        /// and - more importantly - so nothing writes over them: a file that cannot be read today may
        /// still be somebody's history, and the reason may be temporary.
        /// </summary>
        private static readonly HashSet<Guid> Unreadable = new();

        private static readonly SemaphoreSlim Gate = new(1, 1);

        /// <summary>Conversations about a query plan: this plan's first, then the same query's.</summary>
        public static async Task<IReadOnlyList<StoredConversation>> ForPlanAsync(
            string? signature, string? planHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(signature)) return Array.Empty<StoredConversation>();

            var all = await LoadAsync(cancellationToken);

            return all
                .Where(c => c.Kind == Artifact.QueryPlan && Same(c.Signature, signature))
                .OrderByDescending(c => Same(c.ArtifactHash, planHash))
                .ThenByDescending(c => c.UpdatedUtc)
                .ToList();
        }

        /// <summary>
        /// Conversations about a deadlock: this occurrence's first, then its pattern's.
        ///
        /// Matched on either identity, as AI.DeadlockAnalysisHistory_Get is - the occurrence hash finds
        /// this exact deadlock whatever has happened to signature versions since, and the signature
        /// finds the rest of the pattern on a best-effort basis.
        /// </summary>
        public static async Task<IReadOnlyList<StoredConversation>> ForDeadlockAsync(
            string? signature, string? deadlockHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(signature) && string.IsNullOrWhiteSpace(deadlockHash))
            {
                return Array.Empty<StoredConversation>();
            }

            var all = await LoadAsync(cancellationToken);

            return all
                .Where(c => c.Kind == Artifact.Deadlock &&
                            (Same(c.Signature, signature) || Same(c.ArtifactHash, deadlockHash)))
                .OrderByDescending(c => Same(c.ArtifactHash, deadlockHash))
                .ThenByDescending(c => c.UpdatedUtc)
                .ToList();
        }

        /// <summary>
        /// Records a follow-up, creating the conversation's local record if this is its first.
        ///
        /// Best effort in the sense that it reports rather than throws - the answer is already on the
        /// reader's screen, and losing it because a file would not write would be the worse outcome.
        /// It does have to report, though: unlike a repository write there is no service quietly
        /// keeping a second copy, so a failure here means this exchange exists nowhere but the window
        /// it is in.
        /// </summary>
        /// <returns>What to tell the user, or null when it was stored.</returns>
        public static async Task<string?> AppendTurnAsync(
            Guid conversationId,
            Artifact kind,
            string? signature,
            string? artifactHash,
            string? instance,
            string? statementText,
            string? payloadVersion,
            StoredTurn turn,
            CancellationToken cancellationToken = default)
        {
            await Gate.WaitAsync(cancellationToken);

            try
            {
                // Read from its own file rather than from the scan, so a turn added by another copy of
                // the application since this one started is built on rather than overwritten.
                var conversation = await ReadCurrentAsync(conversationId, cancellationToken);

                if (conversation is null && Unreadable.Contains(conversationId))
                {
                    // Its file is there and will not open.  Writing would replace the one copy of
                    // whatever is in it, which is the only outcome here that cannot be undone.
                    return "This answer was not saved: an earlier conversation file could not be opened, " +
                           "and overwriting it would lose it.";
                }

                conversation ??= new StoredConversation
                {
                    ConversationId = conversationId,
                    Kind = kind,
                    Signature = signature,
                    ArtifactHash = artifactHash,
                    Instance = instance,
                    StatementText = statementText,
                    PayloadVersion = payloadVersion
                };

                conversation.Turns.Add(turn);
                conversation.UpdatedUtc = DateTime.UtcNow;

                await SaveAsync(conversation, cancellationToken);
                return null;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not store a follow-up conversation turn");
                return "This answer was not saved: " + ex.Message;
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>Everything held locally, for the export dialog to describe and write out.</summary>
        public static async Task<IReadOnlyList<StoredConversation>> AllAsync(
            CancellationToken cancellationToken = default) =>
            await LoadAsync(cancellationToken);

        /// <summary>
        /// Every conversation as one document, optionally wrapped under a passphrase.
        ///
        /// Plain JSON when no passphrase is given, and that is a feature rather than an oversight: the
        /// user asked for their own conversations in a file, and one they can read, search and keep is
        /// more use than one only this application can open.  It is their data leaving by their
        /// instruction.  A passphrase is there for when the file is going somewhere they would rather
        /// it were not readable.
        /// </summary>
        public static async Task<byte[]> ExportAsync(
            string? passphrase, CancellationToken cancellationToken = default)
        {
            var all = await LoadAsync(cancellationToken);
            var json = JsonSerializer.SerializeToUtf8Bytes(all, Json);

            return string.IsNullOrEmpty(passphrase) ? json : PassphraseProtection.Wrap(json, passphrase);
        }

        public sealed record ImportResult(int Added, int Updated, int Skipped, string? Error)
        {
            public bool Success => Error is null;
        }

        /// <summary>
        /// Takes an exported file and merges it into what is already here.
        ///
        /// Merged rather than replaced, because the usual reason to import is a second machine that has
        /// conversations of its own.  A conversation already present is replaced only when the imported
        /// copy has more turns - which is the same conversation carried on elsewhere - and otherwise
        /// left alone, so importing an older export cannot roll anything back.
        /// </summary>
        public static async Task<ImportResult> ImportAsync(
            byte[] file, string? passphrase, CancellationToken cancellationToken = default)
        {
            byte[] json;

            if (PassphraseProtection.IsWrapped(file))
            {
                if (string.IsNullOrEmpty(passphrase))
                {
                    return new ImportResult(0, 0, 0, "This export is protected by a passphrase.");
                }

                try
                {
                    json = PassphraseProtection.Unwrap(file, passphrase);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    return new ImportResult(0, 0, 0, "That passphrase does not open this file.");
                }
                catch (FormatException ex)
                {
                    return new ImportResult(0, 0, 0, ex.Message);
                }
            }
            else
            {
                json = file;
            }

            List<StoredConversation>? imported;

            try
            {
                imported = JsonSerializer.Deserialize<List<StoredConversation>>(json, Json);
            }
            catch (JsonException)
            {
                return new ImportResult(0, 0, 0, "That file is not a DBA Dash conversation export.");
            }

            if (imported is null) return new ImportResult(0, 0, 0, "That file held no conversations.");

            await Gate.WaitAsync(cancellationToken);

            try
            {
                var all = await LoadUnguardedAsync(cancellationToken);
                var byId = all.ToDictionary(c => c.ConversationId);
                int added = 0, updated = 0, skipped = 0;

                foreach (var conversation in imported.Where(c => c.Turns.Count > 0))
                {
                    // A conversation whose file is here but will not open is left alone rather than
                    // replaced: this import may well be the copy that would have rescued it, but it
                    // may equally be an older one, and there is no way from here to tell.
                    if (Unreadable.Contains(conversation.ConversationId))
                    {
                        skipped++;
                        continue;
                    }

                    if (!byId.TryGetValue(conversation.ConversationId, out var existing))
                    {
                        added++;
                    }
                    else if (conversation.Turns.Count > existing.Turns.Count)
                    {
                        updated++;
                    }
                    else
                    {
                        // An older export of a conversation that has since been carried on - taking it
                        // would roll the conversation back.
                        skipped++;
                        continue;
                    }

                    await SaveAsync(conversation, cancellationToken);
                }

                return new ImportResult(added, updated, skipped, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not import conversations");
                return new ImportResult(0, 0, 0, "The conversations could not be saved: " + ex.Message);
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>Drops what is held in memory, so the next caller reads the files again.</summary>
        internal static void ResetCache()
        {
            Cached.Clear();
            Unreadable.Clear();
        }

        private static async Task<List<StoredConversation>> LoadAsync(CancellationToken cancellationToken)
        {
            await Gate.WaitAsync(cancellationToken);

            try
            {
                return await LoadUnguardedAsync(cancellationToken);
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>
        /// Everything on this machine, reading anything not already held.
        ///
        /// The directory is scanned every time rather than once, because another process may have
        /// written to it since - the cost is one enumeration, and files already read are not read
        /// again.
        /// </summary>
        private static async Task<List<StoredConversation>> LoadUnguardedAsync(CancellationToken cancellationToken)
        {
            if (Directory.Exists(ConversationFolder))
            {
                foreach (var file in Directory.EnumerateFiles(ConversationFolder, "*.dat"))
                {
                    if (!Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var id)) continue;
                    if (Cached.ContainsKey(id) || Unreadable.Contains(id)) continue;

                    var conversation = await ReadAsync(file, cancellationToken);

                    if (conversation is null)
                    {
                        // Left alone from here on.  Nothing writes to this id, so what could not be
                        // opened today survives for a profile - or a DPAPI key - that comes back.
                        Unreadable.Add(id);
                        Log.Warning("A saved AI conversation could not be opened: {file}", file);
                        continue;
                    }

                    Cached[id] = conversation;
                }
            }

            return Cached.Values.ToList();
        }

        /// <summary>
        /// One conversation as it stands on disk right now, ignoring what is cached.
        ///
        /// Always from the file, because another copy of the application may have added a turn since
        /// this one read it - and building on a stale copy would drop that turn.
        /// </summary>
        private static async Task<StoredConversation?> ReadCurrentAsync(
            Guid conversationId, CancellationToken cancellationToken)
        {
            var path = PathFor(conversationId);
            if (!File.Exists(path)) return null;

            var conversation = await ReadAsync(path, cancellationToken);

            if (conversation is null)
            {
                Unreadable.Add(conversationId);
                return null;
            }

            Unreadable.Remove(conversationId);
            Cached[conversationId] = conversation;
            return conversation;
        }

        private static async Task<StoredConversation?> ReadAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                var stored = await File.ReadAllTextAsync(path, cancellationToken);
                if (string.IsNullOrWhiteSpace(stored)) return null;

                var json = OperatingSystem.IsWindows() ? stored.UserDecryptString() : stored;

                return JsonSerializer.Deserialize<StoredConversation>(json, Json);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // A profile copied between machines carries the file but not the DPAPI key that opens
                // it, and a half-written file will not parse.  Either way this is one conversation the
                // reader cannot see, not a reason to fail on the way to showing them a plan.
                //
                // A cancellation is none of those things - the read simply stopped - so it is let
                // through rather than caught here, because a caller that treated it as a bad file
                // would mark a perfectly good conversation Unreadable and stop writing to it.
                Log.Debug(ex, "Could not read a saved AI conversation from {path}", path);
                return null;
            }
        }

        /// <summary>
        /// Writes one conversation, and only that one.
        ///
        /// Written beside its own file and moved into place, so an interrupted write cannot leave a
        /// half-file where a conversation used to be.  The temporary name carries the process id: two
        /// copies of the application saving at the same moment must not collide on it.
        /// </summary>
        private static async Task SaveAsync(StoredConversation conversation, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(ConversationFolder);

            var json = JsonSerializer.Serialize(conversation, Json);
            var stored = OperatingSystem.IsWindows() ? json.UserEncryptString() : json;

            var path = PathFor(conversation.ConversationId);
            var temporary = $"{path}.{Environment.ProcessId}.tmp";

            await File.WriteAllTextAsync(temporary, stored, Encoding.UTF8, cancellationToken);

            File.Move(temporary, path, overwrite: true);

            Cached[conversation.ConversationId] = conversation;
        }

        /// <summary>
        /// Whether two identities match.  Hashes are passed about as "0x..." strings and arrive from
        /// SQL Server, from a parsed plan and from a JSON file, so the comparison is deliberately
        /// case-insensitive rather than trusting them all to agree on it.
        /// </summary>
        private static bool Same(string? left, string? right) =>
            !string.IsNullOrWhiteSpace(left) &&
            !string.IsNullOrWhiteSpace(right) &&
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
