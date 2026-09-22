#nullable enable
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DBADash
{
    /// <summary>
    /// Data wrapped under a passphrase, for the one case where DPAPI cannot help: a file that has to
    /// leave the machine that wrote it.
    ///
    /// DPAPI protects everything DBA Dash keeps locally, and it is the right tool for that - the user
    /// never sees a passphrase and there is nothing to lose.  What it cannot do is travel: a blob
    /// protected to a Windows account is unreadable on the next machine, which is exactly what an
    /// export needs to be readable on.  So an export that the user asks to protect gets a passphrase
    /// instead, and that passphrase is the only thing standing between the file and a reader.
    ///
    /// Salted per call and stretched, so the same passphrase never produces the same bytes twice and
    /// guessing at it costs something.  The iteration count travels in the blob rather than being
    /// assumed, so it can be raised later without stranding files written before it was.
    ///
    /// GCM rather than CBC: it authenticates as well as encrypts, so a wrong passphrase fails the tag
    /// check and can be reported as a wrong passphrase, instead of decrypting into rubbish that the
    /// caller then has to guess about.
    /// </summary>
    public static class PassphraseProtection
    {
        /// <summary>The format byte, so a file written today can still be read when there is a version 2.</summary>
        private const byte Version = 1;

        private const int KeySizeBytes = 32;   // AES-256
        private const int SaltSizeBytes = 16;
        private const int NonceSizeBytes = 12; // GCM's native nonce size
        private const int TagSizeBytes = 16;

        /// <summary>
        /// What guessing at an exported file costs.  Applied once when a user exports or imports, so it
        /// can afford to be slow; OWASP's floor for PBKDF2-SHA256 at the time of writing.
        /// </summary>
        private const int Iterations = 600_000;

        public static byte[] Wrap(byte[] data, string passphrase)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Iterations, HashAlgorithmName.SHA256, KeySizeBytes);
            var ciphertext = new byte[data.Length];
            var tag = new byte[TagSizeBytes];

            try
            {
                using var aes = new AesGcm(key, TagSizeBytes);
                aes.Encrypt(nonce, data, ciphertext, tag);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }

            // version | iterations | salt | nonce | tag | ciphertext
            var blob = new byte[1 + 4 + SaltSizeBytes + NonceSizeBytes + TagSizeBytes + ciphertext.Length];
            var at = 0;

            blob[at++] = Version;
            BinaryPrimitives.WriteInt32LittleEndian(blob.AsSpan(at), Iterations);
            at += 4;
            salt.CopyTo(blob, at);
            at += SaltSizeBytes;
            nonce.CopyTo(blob, at);
            at += NonceSizeBytes;
            tag.CopyTo(blob, at);
            at += TagSizeBytes;
            ciphertext.CopyTo(blob, at);

            return blob;
        }

        /// <summary>
        /// Unwraps what <see cref="Wrap"/> produced.
        ///
        /// A wrong passphrase arrives as <see cref="CryptographicException"/> from the tag check, and
        /// something that is not one of these files at all as <see cref="FormatException"/>.  Callers
        /// tell the user which, because they are different mistakes with different fixes.
        /// </summary>
        public static byte[] Unwrap(byte[] blob, string passphrase)
        {
            if (blob is null || blob.Length < 1 + 4 + SaltSizeBytes + NonceSizeBytes + TagSizeBytes)
            {
                throw new FormatException("Not a DBA Dash export: too short to hold a header.");
            }

            if (blob[0] != Version)
            {
                throw new FormatException(
                    $"This export was written by a newer version of DBA Dash (format {blob[0]}).");
            }

            var at = 1;
            var iterations = BinaryPrimitives.ReadInt32LittleEndian(blob.AsSpan(at));
            at += 4;

            // Read from the file, so it decides how much work this process does before it fails.
            if (iterations is < 1000 or > 10_000_000)
            {
                throw new FormatException("Not a DBA Dash export: the work factor is out of range.");
            }

            var salt = blob.AsSpan(at, SaltSizeBytes).ToArray();
            at += SaltSizeBytes;
            var nonce = blob.AsSpan(at, NonceSizeBytes);
            at += NonceSizeBytes;
            var tag = blob.AsSpan(at, TagSizeBytes);
            at += TagSizeBytes;
            var ciphertext = blob.AsSpan(at);

            var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, iterations, HashAlgorithmName.SHA256, KeySizeBytes);
            var data = new byte[ciphertext.Length];

            try
            {
                using var aes = new AesGcm(key, TagSizeBytes);
                aes.Decrypt(nonce, ciphertext, tag, data);
                return data;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        /// <summary>
        /// Whether <paramref name="blob"/> looks like a wrapped export rather than a plain one.  An
        /// export the user chose not to protect is plain JSON, so import has to tell them apart before
        /// it knows whether to ask for a passphrase.
        /// </summary>
        public static bool IsWrapped(byte[] blob) =>
            blob is { Length: >= 1 + 4 + SaltSizeBytes + NonceSizeBytes + TagSizeBytes } && blob[0] == Version;
    }
}
