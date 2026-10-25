using DBADash;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace DBADashConfig.Test
{
    [TestClass]
    public class SecretProtectionTests
    {
        private const string Password = "SourcePa$$word";
        private const string SourceConnection = "Data Source=SQL1;User ID=dbadash;Password=" + Password;
        private const string Destination = "Data Source=Repo;Initial Catalog=DBADashDB;User ID=dbadash;Password=DestPassword";

        private static CollectionConfig NewConfig(BasicConfig.EncryptionOptions option = BasicConfig.EncryptionOptions.Basic)
        {
            var cfg = new CollectionConfig { Destination = Destination, EncryptionOption = option, SecretKey = "AwsSecret" };
            cfg.SourceConnections.Add(new DBADashSource(SourceConnection));
            return cfg;
        }

        private static string StoredPassword(string connectionString) => new SqlConnectionStringBuilder(connectionString).Password;

        private static string InnerJson(string envelope, string password) => EncryptedConfig.Deserialize(envelope).GetConfig(password);

        [TestMethod]
        public void LegacyObfuscatedPasswordIsReadable()
        {
            var legacy = "Data Source=SQL1;User ID=dbadash;Password=" + Password.EncryptObfuscated();
            var cn = new DBADashConnection(legacy);
            Assert.AreEqual(Password, StoredPassword(cn.ConnectionString));
            Assert.IsNull(cn.SecretError);
        }

        private static string MachineKeyPrefix(BasicConfig cfg) =>
            OperatingSystem.IsWindows() ? SecretProtection.ConfigKeyPrefix + ConfigKey.LoadMachineKey(cfg.MachineKey).Id + ":" : throw new PlatformNotSupportedException();

        [TestMethod]
        public void BasicConfigUsesMachineKeyOnWindows()
        {
            if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("DPAPI is Windows only"); return; }
            var json = NewConfig().Serialize();

            Assert.IsFalse(json.Contains(Password));
            Assert.IsFalse(json.Contains("AwsSecret"));
            var cfg = CollectionConfig.Deserialize(json);
            StringAssert.StartsWith(cfg.MachineKey, SecretProtection.DpApiPrefix);
            StringAssert.StartsWith(StoredPassword(cfg.SourceConnections[0].ConnectionString), MachineKeyPrefix(cfg));
            StringAssert.StartsWith(cfg.SecretKey, MachineKeyPrefix(cfg));
            Assert.AreEqual(Password, StoredPassword(cfg.SourceConnections[0].SourceConnection.ConnectionString));
            Assert.AreEqual("AwsSecret", cfg.GetSecretKey());
            Assert.IsFalse(cfg.RequiresSecretUpgrade());
        }

        [TestMethod]
        public void ProtectedPasswordsAreShort()
        {
            if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("DPAPI is Windows only"); return; }
            var cfg = CollectionConfig.Deserialize(NewConfig().Serialize());
            // 14 character password: ~74 characters vs ~330 for a DPAPI blob per secret
            Assert.IsTrue(StoredPassword(cfg.SourceConnections[0].ConnectionString).Length < 80);
        }

        [TestMethod]
        public void MachineKeyIsStableBetweenSaves()
        {
            if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("DPAPI is Windows only"); return; }
            var first = NewConfig().Serialize();
            var second = CollectionConfig.Deserialize(first).Serialize();
            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void MachineKeyFromAnotherMachineIsReportedAndReplaced()
        {
            if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("DPAPI is Windows only"); return; }
            // A key that was never loaded in this process, like one from another machine
            var otherMachineKey = ConfigKey.CreateMachineKey(out _);
            var originalKey = SecretProtection.DpApiPrefix + Convert.ToBase64String(new byte[64]);
            var json = JsonConvert.SerializeObject(new
            {
                Destination = "C:\\Test",
                MachineKey = originalKey,
                SecretKey = SecretProtection.Protect("AwsSecret", otherMachineKey),
                SourceConnections = new[] { new { ConnectionString = "Data Source=SQL1;User ID=dbadash;Password=" + SecretProtection.Protect(Password, otherMachineKey) } }
            });
            var cfg = CollectionConfig.Deserialize(json);

            Assert.AreEqual(2, cfg.GetUnreadableSecrets().Count);

            // Re-entered secrets are protected with a new machine key
            cfg.SourceConnections[0].ConnectionString = SourceConnection;
            var reloaded = CollectionConfig.Deserialize(cfg.Serialize());
            Assert.AreNotEqual(originalKey, reloaded.MachineKey);
            Assert.AreEqual(Password, StoredPassword(reloaded.SourceConnections[0].SourceConnection.ConnectionString));
        }

        [TestMethod]
        public void LegacyObfuscationIsUpgradedToMachineKey()
        {
            if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("DPAPI is Windows only"); return; }
            var legacyJson = JsonConvert.SerializeObject(new
            {
                Destination = "C:\\Test",
                SecretKey = "AwsSecret".EncryptObfuscated(),
                SourceConnections = new[] { new { ConnectionString = "Data Source=SQL1;User ID=dbadash;Password=" + Password.EncryptObfuscated() } }
            });
            var cfg = CollectionConfig.Deserialize(legacyJson);
            Assert.IsTrue(cfg.RequiresSecretUpgrade());

            var upgraded = CollectionConfig.Deserialize(cfg.Serialize());
            Assert.IsFalse(upgraded.RequiresSecretUpgrade());
            StringAssert.StartsWith(StoredPassword(upgraded.SourceConnections[0].ConnectionString), MachineKeyPrefix(upgraded));
            Assert.AreEqual(Password, StoredPassword(upgraded.SourceConnections[0].SourceConnection.ConnectionString));
            Assert.AreEqual("AwsSecret", upgraded.GetSecretKey());
        }

        [TestMethod]
        public void EncryptedConfigProtectsSecretsWithConfigKey()
        {
            const string configPassword = "config password 1";
            var envelope = NewConfig(BasicConfig.EncryptionOptions.Encrypt).Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword);

            var env = JObject.Parse(envelope);
            Assert.AreEqual(EncryptedConfig.CurrentVersion, env.Value<int>("Version"));
            Assert.IsNotNull(env["Salt"]);

            var inner = InnerJson(envelope, configPassword);
            Assert.IsFalse(inner.Contains(Password));
            Assert.IsFalse(inner.Contains(SecretProtection.DpApiPrefix));

            var cfg = CollectionConfig.Deserialize(envelope, configPassword);
            StringAssert.StartsWith(StoredPassword(cfg.SourceConnections[0].ConnectionString), SecretProtection.ConfigKeyPrefix + cfg.EncryptionKey.Id + ":");
            StringAssert.StartsWith(cfg.SecretKey, SecretProtection.ConfigKeyPrefix);
            Assert.AreEqual(Password, StoredPassword(cfg.SourceConnections[0].SourceConnection.ConnectionString));
            Assert.AreEqual("AwsSecret", cfg.GetSecretKey());
            Assert.IsFalse(cfg.RequiresSecretUpgrade());

            // The decrypted JSON shown in the config tool should also use the config key
            Assert.IsFalse(cfg.Serialize().Contains(SecretProtection.DpApiPrefix));
        }

        [TestMethod]
        public void SaltAndSecretsAreStableWhilePasswordIsUnchanged()
        {
            const string configPassword = "config password 2";
            var first = NewConfig(BasicConfig.EncryptionOptions.Encrypt).Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword);
            var cfg = CollectionConfig.Deserialize(first, configPassword);
            var second = cfg.Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword);

            Assert.AreEqual(JObject.Parse(first).Value<string>("Salt"), JObject.Parse(second).Value<string>("Salt"));
            Assert.AreEqual(InnerJson(first, configPassword), InnerJson(second, configPassword));
        }

        [TestMethod]
        public void PasswordChangeReprotectsSecrets()
        {
            var envelope = NewConfig(BasicConfig.EncryptionOptions.Encrypt).Serialize(BasicConfig.EncryptionOptions.Encrypt, "old password");
            var cfg = CollectionConfig.Deserialize(envelope, "old password");
            var oldId = cfg.EncryptionKey.Id;

            var changed = cfg.Serialize(BasicConfig.EncryptionOptions.Encrypt, "new password");
            Assert.ThrowsExactly<ConfigDecryptionError>(() => CollectionConfig.Deserialize(changed, "old password"));

            var reloaded = CollectionConfig.Deserialize(changed, "new password");
            Assert.AreNotEqual(oldId, reloaded.EncryptionKey.Id);
            StringAssert.StartsWith(StoredPassword(reloaded.SourceConnections[0].ConnectionString), SecretProtection.ConfigKeyPrefix + reloaded.EncryptionKey.Id + ":");
            Assert.AreEqual(Password, StoredPassword(reloaded.SourceConnections[0].SourceConnection.ConnectionString));
        }

        [TestMethod]
        public void LegacyEncryptedConfigIsReadableAndUpgraded()
        {
            const string configPassword = "legacy password";
            var innerJson = JsonConvert.SerializeObject(new
            {
                Destination = "C:\\Test",
                EncryptionOption = 1,
                SourceConnections = new[] { new { ConnectionString = "Data Source=SQL1;User ID=dbadash;Password=" + Password.EncryptObfuscated() } }
            });
            var legacyEnvelope = new EncryptedConfig { ProtectedConfig = innerJson.EncryptString(configPassword) }.Serialize();

            var cfg = CollectionConfig.Deserialize(legacyEnvelope, configPassword);
            Assert.IsTrue(cfg.HasLegacyEncryption);
            Assert.IsTrue(cfg.RequiresSecretUpgrade());
            Assert.AreEqual(Password, StoredPassword(cfg.SourceConnections[0].SourceConnection.ConnectionString));

            var upgraded = cfg.Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword);
            Assert.AreEqual(EncryptedConfig.CurrentVersion, JObject.Parse(upgraded).Value<int>("Version"));
            var reloaded = CollectionConfig.Deserialize(upgraded, configPassword);
            Assert.IsFalse(reloaded.HasLegacyEncryption);
            Assert.IsFalse(reloaded.RequiresSecretUpgrade());
            Assert.AreEqual(Password, StoredPassword(reloaded.SourceConnections[0].SourceConnection.ConnectionString));
        }

        [TestMethod]
        public void TurningOffEncryptionReprotectsWithMachineProtection()
        {
            const string configPassword = "config password 3";
            var envelope = NewConfig(BasicConfig.EncryptionOptions.Encrypt).Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword);
            var cfg = CollectionConfig.Deserialize(envelope, configPassword);
            cfg.EncryptionOption = BasicConfig.EncryptionOptions.Basic;

            var json = cfg.Serialize();
            Assert.IsFalse(json.Contains(SecretProtection.ConfigKeyPrefix + cfg.EncryptionKey.Id));
            Assert.IsFalse(json.Contains(Password));
            var reloaded = CollectionConfig.Deserialize(json);
            Assert.AreEqual(Password, StoredPassword(reloaded.SourceConnections[0].SourceConnection.ConnectionString));
            Assert.AreEqual("AwsSecret", reloaded.GetSecretKey());
            if (OperatingSystem.IsWindows())
            {
                StringAssert.StartsWith(StoredPassword(reloaded.SourceConnections[0].ConnectionString), MachineKeyPrefix(reloaded));
            }
        }

        [TestMethod]
        public void EncryptedConfigHasNoMachineKey()
        {
            const string configPassword = "config password 5";
            var cfg = CollectionConfig.Deserialize(NewConfig().Serialize());
            cfg.EncryptionOption = BasicConfig.EncryptionOptions.Encrypt;
            var envelope = cfg.Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword);
            Assert.IsNull(JObject.Parse(InnerJson(envelope, configPassword))["MachineKey"]);
        }

        [TestMethod]
        public void UnreadableSecretIsReportedAndPreserved()
        {
            var unknown = SecretProtection.ConfigKeyPrefix + "00000000:" + Convert.ToBase64String(new byte[40]);
            var cfg = new CollectionConfig { Destination = "C:\\Test" };
            cfg.SourceConnections.Add(new DBADashSource("Data Source=SQL1;User ID=dbadash;Password=" + unknown) { ConnectionID = "SQL1" });

            var src = cfg.SourceConnections[0].SourceConnection;
            Assert.IsNotNull(src.SecretError);
            Assert.AreEqual("", StoredPassword(src.ConnectionString));
            var unreadable = cfg.GetUnreadableSecrets();
            Assert.AreEqual(1, unreadable.Count);
            StringAssert.Contains(unreadable[0], "SQL1");

            // Can't be re-protected, so the stored value is kept for when it can be decrypted again (or re-entered)
            var reloaded = CollectionConfig.Deserialize(cfg.Serialize());
            Assert.AreEqual(unknown, StoredPassword(reloaded.SourceConnections[0].ConnectionString));
        }

        [TestMethod]
        public void DpApiValueFromAnotherMachineIsReported()
        {
            if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("DPAPI is Windows only"); return; }
            // A real DPAPI blob from this machine with the header kept and the rest replaced, as one from another machine looks
            var blob = Convert.FromBase64String("x".MachineEncryptString());
            Array.Fill(blob, (byte)0x5A, 20, blob.Length - 20);
            var notOurs = SecretProtection.DpApiPrefix + Convert.ToBase64String(blob);
            var cn = new DBADashConnection("Data Source=SQL1;User ID=dbadash;Password=" + notOurs);
            StringAssert.Contains(cn.SecretError, "different machine");
        }

        [TestMethod]
        public void SourceJsonRoundTripAfterEncryptedLoad()
        {
            // Collection jobs carry a JSON copy of the source.  ¬=k1: secrets must still decrypt.
            const string configPassword = "config password 4";
            var envelope = NewConfig(BasicConfig.EncryptionOptions.Encrypt).Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword);
            var cfg = CollectionConfig.Deserialize(envelope, configPassword);

            var copy = JsonConvert.DeserializeObject<DBADashSource>(JsonConvert.SerializeObject(cfg.SourceConnections[0]))!;
            Assert.AreEqual(Password, StoredPassword(copy.SourceConnection.ConnectionString));
            Assert.AreEqual(cfg.SourceConnections[0].ConnectionString, copy.ConnectionString);
        }

        [TestMethod]
        public void CopiedConnectionGetsDifferentProtectedValue()
        {
            const string configPassword = "config password 6";
            var cfg = CollectionConfig.Deserialize(NewConfig(BasicConfig.EncryptionOptions.Encrypt).Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword), configPassword);
            // As the config tool's Copy link does: the new connection starts with the already protected connection string
            var copy = cfg.SourceConnections[0].ConnectionString.Replace("SQL1", "SQL2");
            cfg.SourceConnections.Add(new DBADashSource(copy));
            Assert.AreEqual(StoredPassword(cfg.SourceConnections[0].ConnectionString), StoredPassword(cfg.SourceConnections[1].ConnectionString));

            var reloaded = CollectionConfig.Deserialize(cfg.Serialize(BasicConfig.EncryptionOptions.Encrypt, configPassword), configPassword);
            var first = StoredPassword(reloaded.SourceConnections[0].ConnectionString);
            Assert.AreNotEqual(first, StoredPassword(reloaded.SourceConnections[1].ConnectionString));
            Assert.AreEqual(Password, StoredPassword(reloaded.SourceConnections[1].SourceConnection.ConnectionString));
            // The original keeps its value
            Assert.AreEqual(StoredPassword(cfg.SourceConnections[0].ConnectionString), first);
        }

        [TestMethod]
        public void ConnectionsWithUnreadableSecretsAreRemovedFromRunningConfig()
        {
            var unknown = SecretProtection.ConfigKeyPrefix + "00000000:" + Convert.ToBase64String(new byte[40]);
            var cfg = NewConfig();
            cfg.SourceConnections.Add(new DBADashSource("Data Source=SQL2;User ID=dbadash;Password=" + unknown) { ConnectionID = "SQL2" });
            cfg.SecondaryDestinationConnections.Add(new DBADashConnection("Data Source=Repo2;Initial Catalog=DBADashDB;User ID=dbadash;Password=" + unknown));

            var removed = cfg.RemoveConnectionsWithUnreadableSecrets();
            Assert.AreEqual(2, removed.Count);
            Assert.AreEqual(1, cfg.SourceConnections.Count);
            Assert.AreEqual("SQL1", cfg.SourceConnections[0].SourceConnection.DataSource());
            Assert.AreEqual(0, cfg.SecondaryDestinationConnections.Count);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(int.MaxValue)]
        public void InvalidIterationsAreRejected(int iterations)
        {
            var env = JObject.Parse(NewConfig(BasicConfig.EncryptionOptions.Encrypt).Serialize(BasicConfig.EncryptionOptions.Encrypt, "iterations test"));
            env["Iterations"] = iterations;
            var ex = Assert.ThrowsExactly<ConfigDecryptionError>(() => CollectionConfig.Deserialize(env.ToString(), "iterations test"));
            StringAssert.Contains(ex.Message, "Iterations");
        }

        [TestMethod]
        [DataRow("dpapi:hunter2")]
        [DataRow("dpapi:password")] // Valid base64, but not a DPAPI blob
        [DataRow("¬=k1:not a value")]
        [DataRow("¬=k1:12345678:password")] // Valid base64, but too short for the nonce and tag
        public void PasswordThatLooksLikeAPrefixIsProtected(string literal)
        {
            Assert.IsFalse(SecretProtection.IsProtected(literal));
            var cn = new DBADashConnection("Data Source=SQL1;User ID=dbadash;Password=" + literal);
            Assert.IsNull(cn.SecretError);
            Assert.AreEqual(literal, StoredPassword(cn.ConnectionString));
            Assert.AreNotEqual(literal, StoredPassword(cn.EncryptedConnectionString));
        }

        [TestMethod]
        public void ExportEncryptedIsPortableAndLeavesConfigUnchanged()
        {
            var cfg = CollectionConfig.Deserialize(NewConfig().Serialize());
            var before = cfg.Serialize();
            var defaultKey = SecretProtection.DefaultKey;

            var export = cfg.ExportEncrypted("export password");

            Assert.AreEqual(before, cfg.Serialize());
            Assert.AreSame(defaultKey, SecretProtection.DefaultKey);
            Assert.IsTrue(BasicConfig.IsConfigEncrypted(export));
            var imported = CollectionConfig.Deserialize(export, "export password");
            Assert.AreEqual(BasicConfig.EncryptionOptions.Encrypt, imported.EncryptionOption);
            Assert.IsNull(imported.MachineKey);
            Assert.AreEqual(Password, StoredPassword(imported.SourceConnections[0].SourceConnection.ConnectionString));
            Assert.AreEqual("AwsSecret", imported.GetSecretKey());
            Assert.AreEqual(0, imported.GetUnreadableSecrets().Count);
        }

        [TestMethod]
        public void ExportWithoutSecretsListsThemToBeEntered()
        {
            var cfg = CollectionConfig.Deserialize(NewConfig().Serialize());
            var export = cfg.ExportWithoutSecrets();

            Assert.IsFalse(export.Contains(Password));
            Assert.IsFalse(export.Contains("AwsSecret"));
            Assert.IsFalse(export.Contains(SecretProtection.ConfigKeyPrefix));
            Assert.IsNull(JObject.Parse(export)["MachineKey"]);
            // The config itself still has its secrets
            Assert.AreEqual(Password, StoredPassword(cfg.SourceConnections[0].SourceConnection.ConnectionString));

            var imported = CollectionConfig.Deserialize(export);
            var unreadable = imported.GetSecrets().Where(s => !s.IsReadable).ToList();
            Assert.AreEqual(3, unreadable.Count); // Destination, source and AWS secret key
            StringAssert.Contains(unreadable[0].Error, "wasn't included");
            Assert.AreEqual(1, imported.RemoveConnectionsWithUnreadableSecrets().Count); // The source - the destination stops the service starting

            var reimported = CollectionConfig.Deserialize(export);
            foreach (var secret in reimported.GetSecrets()) secret.SetSecret("entered");
            var saved = CollectionConfig.Deserialize(reimported.Serialize());
            Assert.AreEqual(0, saved.GetUnreadableSecrets().Count);
            Assert.AreEqual("entered", StoredPassword(saved.SourceConnections[0].SourceConnection.ConnectionString));
            Assert.AreEqual("entered", saved.GetSecretKey());
        }

        [TestMethod]
        public void SecondaryDestinationPasswordIsUpgraded()
        {
            // Other platforms keep ¬=! when the config isn't encrypted
            if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("DPAPI is Windows only"); return; }
            // The only password is on a secondary destination
            var legacyJson = JsonConvert.SerializeObject(new
            {
                Destination = "C:\\Test",
                SecondaryDestinations = new[] { "Data Source=Repo2;Initial Catalog=DBADashDB;User ID=dbadash;Password=" + Password.EncryptObfuscated() }
            });
            var cfg = CollectionConfig.Deserialize(legacyJson);
            Assert.IsTrue(cfg.ContainsSensitive());
            Assert.IsTrue(cfg.RequiresSecretUpgrade());

            var upgraded = CollectionConfig.Deserialize(cfg.Serialize());
            Assert.IsFalse(upgraded.RequiresSecretUpgrade());
            Assert.IsFalse(upgraded.Serialize().Contains(SecretProtection.ObfuscationPrefix));
            Assert.AreEqual(Password, StoredPassword(upgraded.SecondaryDestinationConnections[0].ConnectionString));
        }

        [TestMethod]
        [DataRow("\u00ac=!not base64!")]
        [DataRow("\u00ac=!AAAA")] // Base64, but not a whole cipher block
        public void DamagedLegacySecretIsReportedNotThrown(string damaged)
        {
            var cn = new DBADashConnection("Data Source=SQL1;User ID=dbadash;Password=" + damaged);
            StringAssert.Contains(cn.SecretError, "damaged");

            var cfg = new CollectionConfig { Destination = "C:\\Test", SecretKey = damaged };
            Assert.AreEqual(1, cfg.GetUnreadableSecrets().Count);
        }

        [TestMethod]
        public void ExportWithoutSecretsHidesBlankPasswordsAndTheyCanBeSetBlankAgain()
        {
            var cfg = NewConfig();
            cfg.SourceConnections.Add(new DBADashSource("Data Source=SQL2;User ID=nopassword") { ConnectionID = "SQL2" });
            var export = cfg.ExportWithoutSecrets();

            // The blank password is removed like the others, so the export doesn't show which connections have one
            var imported = CollectionConfig.Deserialize(export);
            var blank = imported.GetSecrets().Single(s => s.Name == "SQL2");
            Assert.IsFalse(blank.IsReadable);
            Assert.AreEqual(SecretProtection.RemovedSecret, StoredPassword(imported.SourceConnections[1].ConnectionString));

            blank.SetSecret("");
            var saved = CollectionConfig.Deserialize(imported.Serialize());
            var src = saved.SourceConnections.Single(s => s.ConnectionID == "SQL2").SourceConnection;
            Assert.IsNull(src.SecretError);
            Assert.AreEqual("", StoredPassword(src.ConnectionString));
            Assert.IsTrue(saved.GetSecrets().Single(s => s.Name == "SQL2").IsReadable);
        }

        [TestMethod]
        public void MachineKeyThatCantBeLoadedIsKeptUntilAPasswordIsEntered()
        {
            if (!OperatingSystem.IsWindows()) { Assert.Inconclusive("DPAPI is Windows only"); return; }
            // As copied from another machine: a machine key that can't be loaded here, and a secret that needs it
            var otherMachineKey = ConfigKey.CreateMachineKey(out _);
            var foreignKey = SecretProtection.DpApiPrefix + Convert.ToBase64String(new byte[64]);
            var json = JsonConvert.SerializeObject(new
            {
                Destination = "C:\\Test",
                MachineKey = foreignKey,
                SourceConnections = new[] { new { ConnectionString = "Data Source=SQL1;User ID=dbadash;Password=" + SecretProtection.Protect(Password, otherMachineKey), ConnectionID = "SQL1" } }
            });
            var cfg = CollectionConfig.Deserialize(json);

            // Saving without entering the password keeps the key the secret needs
            var saved = CollectionConfig.Deserialize(cfg.Serialize());
            Assert.AreEqual(foreignKey, saved.MachineKey);
            Assert.IsFalse(saved.RequiresSecretUpgrade());

            // Once a password is entered it needs a key that works here
            saved.GetSecrets().Single().SetSecret(Password);
            var reentered = CollectionConfig.Deserialize(saved.Serialize());
            Assert.AreNotEqual(foreignKey, reentered.MachineKey);
            Assert.AreEqual(Password, StoredPassword(reentered.SourceConnections[0].SourceConnection.ConnectionString));
        }

        [TestMethod]
        public void SetSecretRejectsProtectedValues()
        {
            var cfg = CollectionConfig.Deserialize(NewConfig().Serialize());
            var protectedValue = SecretProtection.Protect("other", ConfigKey.Create("another config"));
            foreach (var secret in cfg.GetSecrets())
            {
                Assert.ThrowsExactly<ArgumentException>(() => secret.SetSecret(protectedValue));
                Assert.ThrowsExactly<ArgumentException>(() => secret.SetSecret(SecretProtection.RemovedSecret));
            }
            Assert.AreEqual(0, cfg.GetUnreadableSecrets().Count);
        }

        [TestMethod]
        public void ManagedIdentityIsNotASecret()
        {
            var cfg = NewConfig();
            const string managedIdentity = "Data Source=az1.database.windows.net;Initial Catalog=DB1;Authentication=Active Directory Managed Identity;User ID=11111111-2222-3333-4444-555555555555";
            cfg.SourceConnections.Add(new DBADashSource(managedIdentity) { ConnectionID = "AZ1" });
            Assert.IsFalse(cfg.GetSecrets().Any(s => s.Name == "AZ1"));

            var imported = CollectionConfig.Deserialize(cfg.ExportWithoutSecrets());
            var src = imported.SourceConnections.Single(s => s.ConnectionID == "AZ1").SourceConnection;
            Assert.IsNull(src.SecretError);
            Assert.AreEqual("", StoredPassword(src.ConnectionString));
        }

        [TestMethod]
        public void ExportWithoutSecretsRefusesConnectionStringsThatCantBeParsed()
        {
            var cfg = NewConfig();
            // An unsupported keyword - the string is kept as it is, password and all
            cfg.SourceConnections.Add(new DBADashSource("Data Source=SQL2;User ID=dbadash;Password=" + Password + ";NotAKeyword=1") { ConnectionID = "SQL2" });
            Assert.AreEqual(DBADashConnection.ConnectionType.Invalid, cfg.SourceConnections[1].SourceConnection.Type);

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => cfg.ExportWithoutSecrets());
            StringAssert.Contains(ex.Message, "SQL2");
            Assert.IsFalse(ex.Message.Contains(Password));
        }

        [TestMethod]
        public void FindConnectionSecretsRequiresAUniqueConnectionID()
        {
            var cfg = new CollectionConfig { Destination = "Data Source=Repo;Initial Catalog=DBADashDB;User ID=repo;Password=RepoPassword" };
            // A source whose ConnectionID is the same as the destination's server|database name
            cfg.SourceConnections.Add(new DBADashSource("Data Source=SQL1;User ID=dbadash;Password=" + Password) { ConnectionID = "Repo|DBADashDB" });
            cfg.SourceConnections.Add(new DBADashSource("Data Source=SQL2;User ID=dbadash;Password=" + Password) { ConnectionID = "SQL2" });

            Assert.ThrowsExactly<InvalidOperationException>(() => cfg.FindConnectionSecrets("Repo|DBADashDB", null));
            Assert.AreEqual(ConfigSecret.SecretTypes.Destination, cfg.FindConnectionSecrets("Repo|DBADashDB", "repo").Single().SecretType);
            Assert.AreEqual(1, cfg.FindConnectionSecrets("sql2", null).Count);
            // A user name on its own can match many
            Assert.AreEqual(2, cfg.FindConnectionSecrets(null, "dbadash").Count);
        }

        [TestMethod]
        public void BlankPasswordIsNotSensitive()
        {
            var cfg = new CollectionConfig { Destination = "C:\\Test" };
            cfg.SourceConnections.Add(new DBADashSource("Data Source=SQL1;User ID=dbadash") { ConnectionID = "SQL1" });
            Assert.AreEqual(1, cfg.GetSecrets().Count); // Still listed, so a password can be set
            Assert.IsFalse(cfg.ContainsSensitive());
        }

        [TestMethod]
        public void SecretsExcludeConnectionsWithoutPasswords()
        {
            var cfg = NewConfig();
            cfg.SourceConnections.Add(new DBADashSource("Data Source=SQL2;Integrated Security=True") { ConnectionID = "SQL2" });
            var secrets = cfg.GetSecrets();
            CollectionAssert.AreEquivalent(new[] { ConfigSecret.SecretTypes.Destination, ConfigSecret.SecretTypes.Source, ConfigSecret.SecretTypes.AWSSecretKey },
                secrets.Select(s => s.SecretType).ToList());
            Assert.IsTrue(secrets.All(s => s.IsReadable));
        }

        [TestMethod]
        public void SetPasswordReplacesAReadablePassword()
        {
            var cfg = CollectionConfig.Deserialize(NewConfig().Serialize());
            cfg.GetSecrets().Single(s => s.SecretType == ConfigSecret.SecretTypes.Source).SetSecret("rotated");
            var saved = CollectionConfig.Deserialize(cfg.Serialize());
            Assert.AreEqual("rotated", StoredPassword(saved.SourceConnections[0].SourceConnection.ConnectionString));
            Assert.IsTrue(SecretProtection.IsProtected(StoredPassword(saved.SourceConnections[0].ConnectionString)));
        }

        [TestMethod]
        public void ConfigKeySecretsUseRandomNonce()
        {
            var key = ConfigKey.Create("nonce test");
            var a = SecretProtection.Protect("same", key);
            var b = SecretProtection.Protect("same", key);
            Assert.AreNotEqual(a, b);
            SecretProtection.UseKey(key);
            Assert.AreEqual("same", SecretProtection.Unprotect(a));
            Assert.AreEqual("same", SecretProtection.Unprotect(b));
        }

        [TestMethod]
        public void TamperedConfigKeySecretFails()
        {
            var key = ConfigKey.Create("tamper test");
            SecretProtection.UseKey(key);
            var value = SecretProtection.Protect("secret", key);
            var bytes = Convert.FromBase64String(value[(SecretProtection.ConfigKeyPrefix.Length + key.Id.Length + 1)..]);
            bytes[^1] ^= 1;
            var tampered = SecretProtection.ConfigKeyPrefix + key.Id + ":" + Convert.ToBase64String(bytes);
            Assert.ThrowsExactly<SecretUnprotectException>(() => SecretProtection.Unprotect(tampered));
        }
    }
}
