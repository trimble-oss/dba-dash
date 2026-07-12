using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DBADash.Test
{
    [TestClass]
    public class CommandTimeoutLegacyNameTests
    {
        [TestMethod]
        public void LegacyDriversWMI_Timeout_IsPreserved()
        {
            // A commandTimeouts.json saved before the DriversWMI -> Drivers rename.
            const string legacyJson = "{\"CollectionCommandTimeouts\":{\"DriversWMI\":300}}";

            var remapped = CollectionCommandTimeout.RemapLegacyNames(legacyJson);
            var settings = JsonConvert.DeserializeObject<CollectionCommandTimeout.CommandTimeoutSettingsBase>(remapped)!;

            Assert.IsTrue(settings.CollectionCommandTimeouts.TryGetValue(CollectionType.Drivers, out var timeout),
                "Legacy DriversWMI timeout should be preserved under Drivers");
            Assert.AreEqual(300, timeout);
        }

        [TestMethod]
        public void CurrentDriversTimeout_StillLoads()
        {
            const string json = "{\"CollectionCommandTimeouts\":{\"Drivers\":300}}";

            var remapped = CollectionCommandTimeout.RemapLegacyNames(json);
            var settings = JsonConvert.DeserializeObject<CollectionCommandTimeout.CommandTimeoutSettingsBase>(remapped)!;

            Assert.AreEqual(300, settings.CollectionCommandTimeouts[CollectionType.Drivers]);
        }

        [TestMethod]
        public void TryParse_HandlesLegacyAndCurrentNames()
        {
            Assert.IsTrue(CollectionTypeLegacyNames.TryParse("DriversWMI", out var legacy));
            Assert.AreEqual(CollectionType.Drivers, legacy);

            Assert.IsTrue(CollectionTypeLegacyNames.TryParse("Drivers", out var current));
            Assert.AreEqual(CollectionType.Drivers, current);

            Assert.IsFalse(CollectionTypeLegacyNames.TryParse("NotACollectionType", out _));
        }
    }
}
