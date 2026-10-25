using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;

namespace DBADash.Messaging
{
    /// <summary>
    /// Encrypts secrets the GUI sends to the service in a message - a connection string with a password - so they aren't
    /// readable in the Service Broker queue or SQS on the way.  The password "encryption" in ServiceConfig.json is only
    /// obfuscation with a key in the source code, so it's no protection here.
    ///
    /// <para>The service creates an RSA key pair when it starts (<see cref="Initialize"/>) and publishes the public key
    /// with its registration in the repository (dbo.DBADashAgent.PublicKey).  The GUI encrypts to that key: a random AES
    /// key encrypts the value (AES-GCM) and RSA-OAEP encrypts the AES key.  Only the running service holds the private
    /// key and it never leaves memory, so a value encrypted before the service restarted can't be decrypted - the GUI
    /// reads the new public key and tries again.</para>
    /// </summary>
    public static class MessageEncryption
    {
        private const string Prefix = "DBADashEnc1:";
        private const int KeySizeBits = 3072;
        private static RSA serviceKey;
        private static readonly object KeyLock = new();

        /// <summary>The service's public key (SubjectPublicKeyInfo, base64) - null until <see cref="Initialize"/> is called, so other processes don't publish one.</summary>
        public static string PublicKey { get; private set; }

        /// <summary>When <see cref="PublicKey"/> was created (UTC) - so the repository can ignore a key older than the one it has.</summary>
        public static DateTime? PublicKeyCreatedUtc { get; private set; }

        /// <summary>Creates the service's key pair.  Called once by the service at startup.</summary>
        public static void Initialize()
        {
            lock (KeyLock)
            {
                if (serviceKey != null) return;
                serviceKey = RSA.Create(KeySizeBits);
                PublicKey = Convert.ToBase64String(serviceKey.ExportSubjectPublicKeyInfo());
                PublicKeyCreatedUtc = DateTime.UtcNow;
            }
        }

        /// <summary>True if <paramref name="value"/> was encrypted by <see cref="Encrypt"/></summary>
        public static bool IsEncrypted(string value) => value != null && value.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>Encrypts <paramref name="plainText"/> for the service that published <paramref name="publicKey"/>.</summary>
        public static string Encrypt(string plainText, string publicKey)
        {
            ArgumentNullException.ThrowIfNull(plainText);
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("The service hasn't published an encryption key.  Upgrade the service, or check it's running.", nameof(publicKey));
            }

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);

            var key = RandomNumberGenerator.GetBytes(32);
            var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
            var plain = Encoding.UTF8.GetBytes(plainText);
            var cipher = new byte[plain.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];
            using (var aes = new AesGcm(key, tag.Length))
            {
                aes.Encrypt(nonce, plain, cipher, tag);
            }

            var envelope = new Envelope
            {
                Key = Convert.ToBase64String(rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256)),
                Nonce = Convert.ToBase64String(nonce),
                Tag = Convert.ToBase64String(tag),
                CipherText = Convert.ToBase64String(cipher)
            };
            return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(envelope)));
        }

        /// <summary>Decrypts a value encrypted with this service's public key.</summary>
        public static string Decrypt(string value)
        {
            if (!IsEncrypted(value))
            {
                throw new ArgumentException("The value isn't encrypted.");
            }

            RSA key;
            lock (KeyLock)
            {
                key = serviceKey ?? throw new InvalidOperationException("The service's encryption key hasn't been created.");
            }

            try
            {
                var envelope = JsonConvert.DeserializeObject<Envelope>(Encoding.UTF8.GetString(Convert.FromBase64String(value[Prefix.Length..])))
                               ?? throw new CryptographicException("Empty envelope");
                var aesKey = key.Decrypt(Convert.FromBase64String(envelope.Key), RSAEncryptionPadding.OaepSHA256);
                var cipher = Convert.FromBase64String(envelope.CipherText);
                var tag = Convert.FromBase64String(envelope.Tag);
                var plain = new byte[cipher.Length];
                using var aes = new AesGcm(aesKey, tag.Length);
                aes.Decrypt(Convert.FromBase64String(envelope.Nonce), cipher, tag, plain);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException)
            {
                throw new CryptographicException("The value couldn't be decrypted.  It may have been encrypted for an earlier run of the service - try again.", ex);
            }
        }

        private sealed class Envelope
        {
            [JsonProperty("k")] public string Key { get; set; }
            [JsonProperty("n")] public string Nonce { get; set; }
            [JsonProperty("t")] public string Tag { get; set; }
            [JsonProperty("c")] public string CipherText { get; set; }
        }
    }
}
