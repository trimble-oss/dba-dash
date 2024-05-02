using DBADash;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DBADash.Messaging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DBADashConfig.Test
{
    [TestClass]
    public class EncryptTest
    {
        [TestMethod]
        [DataRow("secret text", "pass phrase 123")]
        [DataRow("ThisIsSomeSecretTextPleaseEncryptMe", "ThePassPhrase")]
        [DataRow(
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
            "the passphrase used to decrypt")]
        public void TestEncryption(string textToEncrypt, string passPhrase)
        {
            var encrypted = textToEncrypt.EncryptString(passPhrase);
            var decrypted = encrypted.DecryptString(passPhrase);

            Assert.AreEqual(textToEncrypt, decrypted);
            Assert.AreNotEqual(textToEncrypt, encrypted);
            try
            {
                var decryptedFail = encrypted.DecryptString("wrong pass");
                throw new Exception("Decryption should fail with wrong password");
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                Console.WriteLine("Expected Error for wrong password:" + ex.Message);
                // Expected error
            }
        }

        [TestMethod]
        public void TestCollectionConfigDecryption()
        {
            CollectionConfig cfg = new()
            {
                SecretKey = "¬=!dxlFmeasnXObWkKxadBkxF3pIKu5dkpvL6frRLrTgDA="
            };
            var key = cfg.GetSecretKey();
            Assert.AreEqual(key, "ThisIsWhatTheTextShouldBe");
        }

        [TestMethod]
        [DataRow("C:\\Test", "pass phrase XYZ")]
        public void TestFullEncryption(string dest, string passPhrase)
        {
            var cfg = new CollectionConfig
            {
                Destination = dest,
                EncryptionOption = BasicConfig.EncryptionOptions.Encrypt
            };
            var protectedCfg = cfg.Serialize(BasicConfig.EncryptionOptions.Encrypt, passPhrase);

            Assert.IsFalse(protectedCfg.Contains(dest));
            Assert.IsTrue(BasicConfig.IsConfigEncrypted(protectedCfg));
            var deserializedCfg = CollectionConfig.Deserialize(protectedCfg, passPhrase);
            Assert.IsTrue(deserializedCfg.Destination == dest);
        }

        [TestMethod]
        public void EncryptionShouldFailWithoutPassword()
        {
            EncryptedConfig.ClearPassword();
            var cfg = new CollectionConfig
            {
                EncryptionOption = BasicConfig.EncryptionOptions.Encrypt
            };

            Assert.ThrowsException<ArgumentNullException>(() => cfg.Serialize(BasicConfig.EncryptionOptions.Encrypt));
        }

        [TestMethod]
        public void TestSerialization()
        {
            var x = new CollectionMessage(new CollectionType[] { CollectionType.Drives, CollectionType.TableSize }, "TEST");
            var payload = x.Serialize();

            var y = MessageBase.Deserialize(payload);
        }
    }
}