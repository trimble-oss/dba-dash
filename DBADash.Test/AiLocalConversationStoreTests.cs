using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.AiLocalConversationStore;

namespace DBADash.Test
{
    /// <summary>
    /// The store that holds a reader's own AI follow-ups on their machine.
    ///
    /// What these guard against is losing them.  There is no second copy anywhere - that is the whole
    /// point of the design - so a conversation this store drops, mismatches or overwrites is gone. The
    /// cases that matter are finding the right conversation again, and an import that merges rather
    /// than trampling what is already here.
    /// </summary>
    [TestClass]
    public class AiLocalConversationStoreTests
    {
        private string _folder = null!;

        [TestInitialize]
        public void SetUp()
        {
            // Never the real profile: these tests write, and the profile belongs to whoever is running
            // them.
            _folder = Path.Combine(Path.GetTempPath(), "DBADashTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            FolderOverride = _folder;
            ResetCache();
        }

        [TestCleanup]
        public void TearDown()
        {
            FolderOverride = null;
            ResetCache();

            try
            {
                if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
            }
            catch (IOException)
            {
                // A temp folder left behind is not worth failing a test over.
            }
        }

        private static StoredTurn Turn(string question = "why the key lookup?", string answer = "Because.") =>
            new(question, answer, "some-model", DateTime.UtcNow);

        private static Task<string?> AddPlanTurnAsync(
            Guid id, string signature, string planHash, StoredTurn? turn = null) =>
            AppendTurnAsync(id, Artifact.QueryPlan, signature, planHash, "SQL01", "SELECT 1", "2-xml",
                turn ?? Turn());

        [TestMethod]
        public async Task ATurnIsStoredAndReadBack()
        {
            var id = Guid.NewGuid();

            Assert.IsNull(await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222"));

            var found = await ForPlanAsync("0x1111111111111111", "0x2222222222222222");

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual(id, found[0].ConversationId);
            Assert.AreEqual("why the key lookup?", found[0].Turns[0].Question);
            Assert.AreEqual("Because.", found[0].Turns[0].Answer);
        }

        /// <summary>It has to survive the process, or it is a cache rather than a store.</summary>
        [TestMethod]
        public async Task TurnsSurviveAReload()
        {
            var id = Guid.NewGuid();
            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222");

            ResetCache();

            var found = await ForPlanAsync("0x1111111111111111", "0x2222222222222222");

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual(id, found[0].ConversationId);
        }

        [TestMethod]
        public async Task SeveralTurnsAccumulateOnOneConversation()
        {
            var id = Guid.NewGuid();

            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("first", "a"));
            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("second", "b"));

            var found = await ForPlanAsync("0x1111111111111111", "0x2222222222222222");

            Assert.AreEqual(1, found.Count);
            CollectionAssert.AreEqual(
                new[] { "first", "second" },
                found[0].Turns.Select(t => t.Question).ToArray());
        }

        /// <summary>Another query's conversation must not turn up under this one.</summary>
        [TestMethod]
        public async Task AnotherQueryIsNotReturned()
        {
            await AddPlanTurnAsync(Guid.NewGuid(), "0x1111111111111111", "0x2222222222222222");

            Assert.AreEqual(0, (await ForPlanAsync("0x9999999999999999", "0x2222222222222222")).Count);
        }

        /// <summary>A deadlock conversation is not a plan conversation, whatever the hashes say.</summary>
        [TestMethod]
        public async Task TheTwoViewersDoNotSeeEachOthersConversations()
        {
            await AppendTurnAsync(Guid.NewGuid(), Artifact.Deadlock, "0x1111111111111111",
                "0x33333333333333333333333333333333", "SQL01", null, "1", Turn());

            Assert.AreEqual(0, (await ForPlanAsync("0x1111111111111111", null)).Count);
            Assert.AreEqual(1, (await ForDeadlockAsync("0x1111111111111111", null)).Count);
        }

        /// <summary>A conversation about this exact plan sorts ahead of one about the same query.</summary>
        [TestMethod]
        public async Task ThisPlansConversationComesFirst()
        {
            var otherPlan = Guid.NewGuid();
            var thisPlan = Guid.NewGuid();

            await AddPlanTurnAsync(otherPlan, "0x1111111111111111", "0xAAAAAAAAAAAAAAAA");
            await AddPlanTurnAsync(thisPlan, "0x1111111111111111", "0xBBBBBBBBBBBBBBBB");

            var found = await ForPlanAsync("0x1111111111111111", "0xAAAAAAAAAAAAAAAA");

            Assert.AreEqual(2, found.Count);
            Assert.AreEqual(otherPlan, found[0].ConversationId);
        }

        /// <summary>
        /// The reason deadlock conversations carry the occurrence hash: a signature version change
        /// moves the repository's rows and cannot move these, so a conversation found only by signature
        /// would be lost. Found by hash, it survives.
        /// </summary>
        [TestMethod]
        public async Task ADeadlockConversationIsFoundByItsOccurrenceHashAfterItsSignatureChanges()
        {
            var id = Guid.NewGuid();

            await AppendTurnAsync(id, Artifact.Deadlock, "0x1111111111111111",
                "0x33333333333333333333333333333333", "SQL01", null, "1", Turn());

            // The pattern has been recomputed under a new signature version.
            var found = await ForDeadlockAsync("0x7777777777777777", "0x33333333333333333333333333333333");

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual(id, found[0].ConversationId);
        }

        /// <summary>Hashes reach this from SQL Server, a parsed plan and a JSON file - casing is not guaranteed.</summary>
        [TestMethod]
        public async Task IdentitiesMatchRegardlessOfCase()
        {
            await AddPlanTurnAsync(Guid.NewGuid(), "0xABCDEF0123456789", "0xFEDCBA9876543210");

            Assert.AreEqual(1, (await ForPlanAsync("0xabcdef0123456789", "0xfedcba9876543210")).Count);
        }

        [TestMethod]
        public async Task ExportAndImportRoundTripToAnotherMachine()
        {
            var id = Guid.NewGuid();
            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222");

            var exported = await ExportAsync(passphrase: null);

            // The other machine: a store with nothing in it.
            var elsewhere = Path.Combine(_folder, "elsewhere");
            Directory.CreateDirectory(elsewhere);
            FolderOverride = elsewhere;
            ResetCache();

            Assert.AreEqual(0, (await ForPlanAsync("0x1111111111111111", "0x2222222222222222")).Count);

            var result = await ImportAsync(exported, passphrase: null);

            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(1, result.Added);

            var found = await ForPlanAsync("0x1111111111111111", "0x2222222222222222");
            Assert.AreEqual(1, found.Count);
            Assert.AreEqual(id, found[0].ConversationId);
        }

        [TestMethod]
        public async Task AProtectedExportRoundTrips()
        {
            await AddPlanTurnAsync(Guid.NewGuid(), "0x1111111111111111", "0x2222222222222222");

            var exported = await ExportAsync("correct horse battery");

            Assert.IsTrue(PassphraseProtection.IsWrapped(exported));

            var elsewhere = Path.Combine(_folder, "elsewhere");
            Directory.CreateDirectory(elsewhere);
            FolderOverride = elsewhere;
            ResetCache();

            Assert.IsTrue((await ImportAsync(exported, "correct horse battery")).Success);
            Assert.AreEqual(1, (await ForPlanAsync("0x1111111111111111", "0x2222222222222222")).Count);
        }

        [TestMethod]
        public async Task AProtectedExportNeedsItsPassphrase()
        {
            await AddPlanTurnAsync(Guid.NewGuid(), "0x1111111111111111", "0x2222222222222222");
            var exported = await ExportAsync("correct horse battery");

            Assert.IsFalse((await ImportAsync(exported, "wrong")).Success);
            Assert.IsFalse((await ImportAsync(exported, passphrase: null)).Success);
        }

        /// <summary>
        /// Import merges: the usual reason to import is a second machine that has conversations of its
        /// own, and they must not be lost to make room.
        /// </summary>
        [TestMethod]
        public async Task ImportKeepsConversationsAlreadyHere()
        {
            var mine = Guid.NewGuid();
            await AddPlanTurnAsync(mine, "0x1111111111111111", "0x2222222222222222");

            // An export made elsewhere, holding a different conversation.
            var elsewhere = Path.Combine(_folder, "elsewhere");
            Directory.CreateDirectory(elsewhere);
            FolderOverride = elsewhere;
            ResetCache();

            var theirs = Guid.NewGuid();
            await AddPlanTurnAsync(theirs, "0x1111111111111111", "0x2222222222222222");
            var exported = await ExportAsync(passphrase: null);

            FolderOverride = _folder;
            ResetCache();

            var result = await ImportAsync(exported, passphrase: null);

            Assert.AreEqual(1, result.Added);

            var found = await ForPlanAsync("0x1111111111111111", "0x2222222222222222");
            CollectionAssert.AreEquivalent(
                new[] { mine, theirs },
                found.Select(c => c.ConversationId).ToArray());
        }

        /// <summary>
        /// Importing an older export of a conversation that has since been carried on must not roll it
        /// back to the shorter version.
        /// </summary>
        [TestMethod]
        public async Task ImportDoesNotShortenAConversation()
        {
            var id = Guid.NewGuid();
            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("first", "a"));

            var older = await ExportAsync(passphrase: null);

            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("second", "b"));

            var result = await ImportAsync(older, passphrase: null);

            Assert.AreEqual(0, result.Added);
            Assert.AreEqual(0, result.Updated);
            Assert.AreEqual(1, result.Skipped);

            var found = await ForPlanAsync("0x1111111111111111", "0x2222222222222222");
            Assert.AreEqual(2, found[0].Turns.Count);
        }

        /// <summary>The same conversation carried on elsewhere replaces the shorter copy here.</summary>
        [TestMethod]
        public async Task ImportExtendsAConversationCarriedOnElsewhere()
        {
            var id = Guid.NewGuid();
            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("first", "a"));

            var elsewhere = Path.Combine(_folder, "elsewhere");
            Directory.CreateDirectory(elsewhere);
            FolderOverride = elsewhere;
            ResetCache();

            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("first", "a"));
            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("second", "b"));
            var exported = await ExportAsync(passphrase: null);

            FolderOverride = _folder;
            ResetCache();

            var result = await ImportAsync(exported, passphrase: null);

            Assert.AreEqual(1, result.Updated);
            Assert.AreEqual(2, (await ForPlanAsync("0x1111111111111111", "0x2222222222222222"))[0].Turns.Count);
        }

        [TestMethod]
        public async Task SomethingThatIsNotAnExportIsRejected()
        {
            var result = await ImportAsync(System.Text.Encoding.UTF8.GetBytes("nonsense"), passphrase: null);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, result.Added);
        }

        /// <summary>
        /// The same protection the store applies, so a test can write a file the store will read.
        /// Guarded exactly as the store guards it - DPAPI is Windows only, and these tests are not.
        /// </summary>
        private static string Protect(string json) =>
            OperatingSystem.IsWindows() ? DBADash.EncryptText.UserEncryptString(json) : json;

        private static string Unprotect(string stored) =>
            OperatingSystem.IsWindows() ? DBADash.EncryptText.UserDecryptString(stored) : stored;

        /// <summary>
        /// The directory holding one file per conversation, as the store lays it out.
        /// </summary>
        private string ConversationFolder => Path.Combine(_folder, "AiConversations");

        /// <summary>
        /// A conversation file that will not open - a profile carried to another machine takes the
        /// file but not the DPAPI key for it.
        /// </summary>
        private string WriteUnreadableFile(Guid id)
        {
            Directory.CreateDirectory(ConversationFolder);
            var path = Path.Combine(ConversationFolder, id.ToString("D") + ".dat");
            File.WriteAllText(path, "not something DPAPI or JSON will accept");
            return path;
        }

        /// <summary>
        /// One unopenable conversation must not stop the rest being read.  Before this was per-file it
        /// would have taken every conversation with it.
        /// </summary>
        [TestMethod]
        public async Task AnUnreadableConversationDoesNotHideTheOthers()
        {
            await AddPlanTurnAsync(Guid.NewGuid(), "0x1111111111111111", "0x2222222222222222");
            WriteUnreadableFile(Guid.NewGuid());
            ResetCache();

            Assert.AreEqual(1, (await ForPlanAsync("0x1111111111111111", "0x2222222222222222")).Count);
        }

        /// <summary>
        /// The one outcome that cannot be undone: writing over a conversation nobody could read.  The
        /// file has to survive, and the caller has to be told why the answer was not saved.
        /// </summary>
        [TestMethod]
        public async Task AnUnreadableConversationIsNotWrittenOver()
        {
            var id = Guid.NewGuid();
            var path = WriteUnreadableFile(id);
            var before = File.ReadAllBytes(path);

            ResetCache();
            await AllAsync(); // The scan is what notices the file will not open.

            var error = await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222");

            Assert.IsNotNull(error, "A turn that could not be saved must say so.");
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path), "The unreadable file was overwritten.");
        }

        /// <summary>
        /// Two copies of the application run at once - the main window and a plan opened from Explorer.
        /// A conversation written by one has to turn up in the other without a restart, which is why
        /// the directory is rescanned rather than read once.
        /// </summary>
        [TestMethod]
        public async Task AConversationWrittenByAnotherProcessIsPickedUp()
        {
            await AddPlanTurnAsync(Guid.NewGuid(), "0x1111111111111111", "0x2222222222222222");
            Assert.AreEqual(1, (await ForPlanAsync("0x1111111111111111", "0x2222222222222222")).Count);

            // The other process writes its own conversation.  No ResetCache: this one is still running.
            await AddPlanTurnAsync(Guid.NewGuid(), "0x1111111111111111", "0x2222222222222222");

            Assert.AreEqual(2, (await ForPlanAsync("0x1111111111111111", "0x2222222222222222")).Count);
        }

        /// <summary>
        /// Appending reads the conversation from its file rather than from what is cached, so a turn
        /// another copy of the application added since is built on instead of being dropped.
        /// </summary>
        [TestMethod]
        public async Task AppendingDoesNotDropATurnAddedElsewhere()
        {
            var id = Guid.NewGuid();
            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("first", "a"));

            // Read it into cache, as showing the plan would.
            await ForPlanAsync("0x1111111111111111", "0x2222222222222222");

            // The other process adds a turn to the same conversation, behind this one's back.
            var path = Path.Combine(ConversationFolder, id.ToString("D") + ".dat");
            var json = Unprotect(File.ReadAllText(path));
            json = json.Replace("\"Turns\": [", "\"Turns\": [\r\n      {\"Question\": \"elsewhere\", \"Answer\": \"b\", \"Model\": \"m\", \"GeneratedUtc\": \"2026-01-01T00:00:00Z\"},");
            File.WriteAllText(path, Protect(json));

            await AddPlanTurnAsync(id, "0x1111111111111111", "0x2222222222222222", Turn("third", "c"));

            ResetCache();
            var found = await ForPlanAsync("0x1111111111111111", "0x2222222222222222");

            Assert.AreEqual(3, found[0].Turns.Count, "The turn written elsewhere was dropped.");
            CollectionAssert.Contains(found[0].Turns.Select(t => t.Question).ToArray(), "elsewhere");
        }

        /// <summary>An empty store is an ordinary state, not an error - it is where every user starts.</summary>
        [TestMethod]
        public async Task AnEmptyStoreReadsAsEmpty()
        {
            Assert.AreEqual(0, (await AllAsync()).Count);
            Assert.AreEqual(0, (await ForPlanAsync("0x1111111111111111", null)).Count);
        }
    }
}
