using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>
    /// The viewers' settings: the typed reader in front of whichever store the host supplies, and the JSON file store the
    /// stand-alone viewer uses.
    /// </summary>
    [TestClass]
    public class ViewerSettingsTests
    {
        private IViewerSettingsStore _original;
        private string _folder;

        [TestInitialize]
        public void Setup()
        {
            _original = ViewerSettings.Store;
            _folder = Path.Combine(Path.GetTempPath(), "DBADashViewerTests_" + Guid.NewGuid().ToString("N"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            ViewerSettings.Store = _original;
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        private string SettingsPath => Path.Combine(_folder, "Visualizer", "settings.json");

        private sealed class DictionaryStore : IViewerSettingsStore
        {
            public Dictionary<string, object> Values { get; } = new();
            public object Get(string name) => Values.GetValueOrDefault(name);
            public void Set(string name, object value) => Values[name] = value;

            public void Save()
            {
            }
        }

        private sealed class ThrowingStore : IViewerSettingsStore
        {
            public object Get(string name) => throw new InvalidOperationException("unreadable");
            public void Set(string name, object value) => throw new InvalidOperationException("unwritable");

            public void Save()
            {
            }
        }

        [TestMethod]
        public void AnEmptyStoreGivesTheDefaults()
        {
            ViewerSettings.Store = new DictionaryStore();

            Assert.AreEqual("Rows", ViewerSettings.QueryPlanEdgeWidth);
            Assert.AreEqual("Ring", ViewerSettings.DeadlockLayoutStyle);
            Assert.AreEqual(3, ViewerSettings.QueryPlanPropertyRowLines);
            Assert.AreEqual(100, ViewerSettings.QueryPlanMinFitZoom);
            Assert.IsTrue(ViewerSettings.QueryPlanShowOperatorDescriptions);
            Assert.IsFalse(ViewerSettings.QueryPlanShowNodeIds);
        }

        [TestMethod]
        public void AStoredValueIsUsed()
        {
            var store = new DictionaryStore();
            ViewerSettings.Store = store;

            ViewerSettings.QueryPlanEdgeWidth = "Cost";
            ViewerSettings.QueryPlanMinFitZoom = 60;

            Assert.AreEqual("Cost", store.Values[nameof(ViewerSettings.QueryPlanEdgeWidth)]);
            Assert.AreEqual("Cost", ViewerSettings.QueryPlanEdgeWidth);
            Assert.AreEqual(60, ViewerSettings.QueryPlanMinFitZoom);
        }

        [TestMethod]
        public void ANumberStoredAsAnotherNumericTypeIsConverted()
        {
            // JSON hands whole numbers back as long.
            ViewerSettings.Store = new DictionaryStore { Values = { [nameof(ViewerSettings.QueryPlanMinFitZoom)] = 75L } };

            Assert.AreEqual(75, ViewerSettings.QueryPlanMinFitZoom);
        }

        [TestMethod]
        public void AValueThatCantBeConvertedFallsBackToTheDefault()
        {
            ViewerSettings.Store = new DictionaryStore { Values = { [nameof(ViewerSettings.QueryPlanMinFitZoom)] = "wide" } };

            Assert.AreEqual(100, ViewerSettings.QueryPlanMinFitZoom);
        }

        [TestMethod]
        public void AStoreThatCantBeReadGivesTheDefault()
        {
            ViewerSettings.Store = new ThrowingStore();

            Assert.AreEqual("Rows", ViewerSettings.QueryPlanEdgeWidth);
        }

        [TestMethod]
        public void TheJsonStoreKeepsSettingsAcrossInstances()
        {
            var first = new JsonFileSettingsStore(SettingsPath);
            first.Set(nameof(ViewerSettings.QueryPlanEdgeWidth), "Cost");
            first.Set(nameof(ViewerSettings.QueryPlanMinFitZoom), 60);
            first.Set(nameof(ViewerSettings.QueryPlanShowNodeIds), true);
            first.Save();

            ViewerSettings.Store = new JsonFileSettingsStore(SettingsPath);

            Assert.AreEqual("Cost", ViewerSettings.QueryPlanEdgeWidth);
            Assert.AreEqual(60, ViewerSettings.QueryPlanMinFitZoom);
            Assert.IsTrue(ViewerSettings.QueryPlanShowNodeIds);
        }

        [TestMethod]
        public void TheJsonStoreCreatesItsFolderAndLeavesNoTemporaryFile()
        {
            var store = new JsonFileSettingsStore(SettingsPath);
            store.Set("Anything", "value");
            store.Save();

            Assert.IsTrue(File.Exists(SettingsPath));
            Assert.IsFalse(File.Exists(SettingsPath + ".tmp"));
        }

        [TestMethod]
        public void AMissingFileGivesTheDefaults()
        {
            ViewerSettings.Store = new JsonFileSettingsStore(SettingsPath);

            Assert.AreEqual("Rows", ViewerSettings.QueryPlanEdgeWidth);
        }

        [TestMethod]
        public void AFileThatIsNotJsonGivesTheDefaultsAndCanBeOverwritten()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            File.WriteAllText(SettingsPath, "this is not json {");

            ViewerSettings.Store = new JsonFileSettingsStore(SettingsPath);
            Assert.AreEqual("Rows", ViewerSettings.QueryPlanEdgeWidth);

            ViewerSettings.QueryPlanEdgeWidth = "Cost";
            ViewerSettings.Save();

            ViewerSettings.Store = new JsonFileSettingsStore(SettingsPath);
            Assert.AreEqual("Cost", ViewerSettings.QueryPlanEdgeWidth);
        }

        [TestMethod]
        public void AFileWithSomethingOtherThanAnObjectGivesTheDefaults()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            File.WriteAllText(SettingsPath, "[1, 2, 3]");

            ViewerSettings.Store = new JsonFileSettingsStore(SettingsPath);

            Assert.AreEqual(3, ViewerSettings.QueryPlanPropertyRowLines);
        }

        [TestMethod]
        public void AnUnwritableLocationDoesNotThrow()
        {
            // A file where the folder should be.
            Directory.CreateDirectory(_folder);
            var blocker = Path.Combine(_folder, "blocked");
            File.WriteAllText(blocker, "content");

            var store = new JsonFileSettingsStore(Path.Combine(blocker, "settings.json"));
            store.Set("Anything", "value");

            store.Save();
        }
    }
}
