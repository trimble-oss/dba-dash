using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The wrapping on an exported conversation file.
    ///
    /// The failure to guard against is silent: a format that does not survive the trip does not produce
    /// a wrong answer, it produces a file the user cannot open on the machine they exported it for -
    /// which is usually the machine they no longer have the original on.
    /// </summary>
    [TestClass]
    public class PassphraseProtectionTests
    {
        private static byte[] Conversations =>
            Encoding.UTF8.GetBytes("""[{"ConversationId":"8f1b...","Turns":[{"Question":"why?"}]}]""");

        [TestMethod]
        public void WrappedDataRoundTrips()
        {
            var wrapped = PassphraseProtection.Wrap(Conversations, "correct horse battery");

            CollectionAssert.AreEqual(Conversations, PassphraseProtection.Unwrap(wrapped, "correct horse battery"));
        }

        [TestMethod]
        public void AWrongPassphraseIsRejected()
        {
            var wrapped = PassphraseProtection.Wrap(Conversations, "correct horse battery");

            Assert.Throws<CryptographicException>(
                () => PassphraseProtection.Unwrap(wrapped, "correct horse batter"));
        }

        /// <summary>The content must not be sitting in the file next to its ciphertext.</summary>
        [TestMethod]
        public void WrappedDataDoesNotCarryThePlaintext()
        {
            var wrapped = PassphraseProtection.Wrap(Conversations, "correct horse battery");

            Assert.IsFalse(Contains(wrapped, Encoding.UTF8.GetBytes("ConversationId")));
        }

        /// <summary>Salted per call, so exporting the same conversations twice does not produce one file twice.</summary>
        [TestMethod]
        public void WrappingTwiceProducesDifferentFiles()
        {
            CollectionAssert.AreNotEqual(
                PassphraseProtection.Wrap(Conversations, "correct horse battery"),
                PassphraseProtection.Wrap(Conversations, "correct horse battery"));
        }

        [TestMethod]
        public void AnAlteredFileIsRejected()
        {
            var wrapped = PassphraseProtection.Wrap(Conversations, "correct horse battery");
            wrapped[^1] ^= 0xFF;

            Assert.Throws<CryptographicException>(
                () => PassphraseProtection.Unwrap(wrapped, "correct horse battery"));
        }

        [TestMethod]
        public void SomethingThatIsNotAnExportIsRejected()
        {
            Assert.Throws<FormatException>(
                () => PassphraseProtection.Unwrap(Encoding.UTF8.GetBytes("not an export"), "x"));
        }

        /// <summary>
        /// Import has to tell a protected export from a plain one before it knows whether to ask for a
        /// passphrase, and a plain export is JSON.
        /// </summary>
        [TestMethod]
        public void PlainJsonIsNotMistakenForAWrappedFile()
        {
            Assert.IsFalse(PassphraseProtection.IsWrapped(Conversations));
            Assert.IsTrue(PassphraseProtection.IsWrapped(
                PassphraseProtection.Wrap(Conversations, "correct horse battery")));
        }

        [TestMethod]
        public void AnEmptyFileIsNotMistakenForAWrappedFile()
        {
            Assert.IsFalse(PassphraseProtection.IsWrapped(Array.Empty<byte>()));
            Assert.IsFalse(PassphraseProtection.IsWrapped(null!));
        }

        /// <summary>Whether <paramref name="needle"/> appears in <paramref name="haystack"/> as a run of bytes.</summary>
        private static bool Contains(byte[] haystack, byte[] needle)
        {
            for (var start = 0; start + needle.Length <= haystack.Length; start++)
            {
                if (haystack.AsSpan(start, needle.Length).SequenceEqual(needle)) return true;
            }

            return false;
        }
    }
}
