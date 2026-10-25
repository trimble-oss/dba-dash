using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DBADash
{
    /// <summary>
    /// Protects secrets (SQL passwords, AWS secret key) stored inside the service config.<br/>
    /// Formats, all of which can be read for backward compatibility:<br/>
    /// ¬=!     Legacy obfuscation.  The passphrase is in the source code so anyone can decrypt it.  Still used on non-Windows platforms when the config isn't encrypted.<br/>
    /// dpapi:  DPAPI LocalMachine.  Only used if a machine key isn't available, as DPAPI values are long.<br/>
    /// ¬=k1:   AES-GCM with a <see cref="ConfigKey"/>.  If the config is encrypted the key is derived from the password, so secrets stay portable with the password.
    ///         Otherwise (Windows) it's a random machine key stored in the config protected with DPAPI LocalMachine, so any process on the machine can decrypt it but it can't be decrypted on another machine.
    /// </summary>
    public static class SecretProtection
    {
        public const string ObfuscationPrefix = EncryptText.ObfuscationPrefix;
        public const string DpApiPrefix = EncryptText.DpApiPrefix;
        public const string ConfigKeyPrefix = "¬=k1:";

        /// <summary>
        /// Stands in for a secret left out of a config exported without secrets.  It's treated as a secret that can't be
        /// decrypted, so it's listed to be re-entered and the connection isn't used until it is.
        /// </summary>
        public const string RemovedSecret = "¬=removed";

        private static readonly ConcurrentDictionary<string, ConfigKey> KeyRing = new();

        /// <summary>
        /// Key used to protect secrets as they are entered.  Set when an encrypted config is loaded or saved.  Secrets are
        /// re-protected to match the config's encryption option when the config is serialized, so this only decides the
        /// format a new secret has until then.
        /// </summary>
        public static ConfigKey DefaultKey { get; private set; }

        /// <summary>
        /// Make <paramref name="key"/> available to decrypt ¬=k1: secrets in this process and use it for new secrets.
        /// </summary>
        internal static void UseKey(ConfigKey key)
        {
            KeyRing[key.Id] = key;
            DefaultKey = key;
        }

        internal static void ClearDefaultKey() => DefaultKey = null;

        internal static void SetDefaultKey(ConfigKey key)
        {
            if (key != null) KeyRing[key.Id] = key;
            DefaultKey = key;
        }

        /// <summary>
        /// True if <paramref name="value"/> is a protected value.  dpapi: and ¬=k1: values must be well formed so a password
        /// that just happens to start with the prefix (e.g. "dpapi:hunter2") is treated as a password.
        /// </summary>
        public static bool IsProtected(string value) =>
            !string.IsNullOrEmpty(value) && (value == RemovedSecret
                                             || value.StartsWith(ObfuscationPrefix, StringComparison.Ordinal)
                                             || IsDpApiValue(value)
                                             || IsConfigKeyValue(value));

        // Every DPAPI blob starts with its version (1) and the DPAPI provider GUID {df9d8cd0-1501-11d1-8c7a-00c04fc297eb}.  The
        // same on every machine, so a blob protected on another machine is still recognized (and reported as unreadable).
        private static readonly byte[] DpApiBlobHeader =
            [0x01, 0x00, 0x00, 0x00, 0xD0, 0x8C, 0x9D, 0xDF, 0x01, 0x15, 0xD1, 0x11, 0x8C, 0x7A, 0x00, 0xC0, 0x4F, 0xC2, 0x97, 0xEB];

        private static bool IsDpApiValue(string value) =>
            value.StartsWith(DpApiPrefix, StringComparison.Ordinal)
            && TryDecodeBase64(value[DpApiPrefix.Length..], out var blob)
            && blob.AsSpan().StartsWith(DpApiBlobHeader);

        // ¬=k1:<8 hex key id>:<base64 nonce|tag|ciphertext>
        private static bool IsConfigKeyValue(string value)
        {
            if (!value.StartsWith(ConfigKeyPrefix, StringComparison.Ordinal)) return false;
            var payload = value[ConfigKeyPrefix.Length..];
            var idx = payload.IndexOf(':');
            return idx == ConfigKey.IdLength && payload[..idx].All(Uri.IsHexDigit)
                                             && TryDecodeBase64(payload[(idx + 1)..], out var bytes)
                                             && bytes.Length >= ConfigKey.MinEncryptedLength;
        }

        private static bool TryDecodeBase64(string value, out byte[] bytes)
        {
            var buffer = new byte[value.Length];
            if (value.Length > 0 && Convert.TryFromBase64String(value, buffer, out var written))
            {
                bytes = buffer[..written];
                return true;
            }
            bytes = null;
            return false;
        }

        /// <summary>
        /// Protect with the config key if specified, otherwise DPAPI LocalMachine (Windows) or obfuscation (other platforms).
        /// </summary>
        public static string Protect(string value, ConfigKey key)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (key != null) return ConfigKeyPrefix + key.Id + ":" + key.EncryptSecret(value);
            if (OperatingSystem.IsWindows()) return DpApiPrefix + value.MachineEncryptString();
            return value.EncryptObfuscated();
        }

        public static string Protect(string value) => Protect(value, DefaultKey);

        /// <summary>
        /// True if <paramref name="value"/> is already in the format <see cref="Protect(string, ConfigKey)"/> would produce for <paramref name="key"/>
        /// </summary>
        public static bool IsProtectedWith(string value, ConfigKey key)
        {
            if (string.IsNullOrEmpty(value)) return true;
            if (key != null) return value.StartsWith(ConfigKeyPrefix + key.Id + ":", StringComparison.Ordinal);
            return value.StartsWith(OperatingSystem.IsWindows() ? DpApiPrefix : ObfuscationPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Decrypt a value in any of the supported formats.  Values without a recognized prefix are returned unchanged.
        /// </summary>
        /// <exception cref="SecretUnprotectException">The value can't be decrypted on this machine/process</exception>
        public static string Unprotect(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value == RemovedSecret) throw new SecretUnprotectException("The password wasn't included when the config was exported.  Enter it.");
            if (value.StartsWith(ObfuscationPrefix, StringComparison.Ordinal)) return UnprotectObfuscated(value);
            if (IsDpApiValue(value)) return UnprotectDpApi(value);
            if (IsConfigKeyValue(value)) return UnprotectConfigKey(value);
            return value;
        }

        private static string UnprotectObfuscated(string value)
        {
            try
            {
                return value.DecryptObfuscated();
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                throw new SecretUnprotectException("The secret is damaged and can't be decrypted.  Re-enter it.", ex);
            }
        }

        private static string UnprotectDpApi(string value)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new SecretUnprotectException("The secret is protected with Windows DPAPI and can't be decrypted on this platform.  Re-enter it.");
            }
            try
            {
                return value[DpApiPrefix.Length..].MachineDecryptString();
            }
            catch (CryptographicException ex)
            {
                throw new SecretUnprotectException("The secret was protected on a different machine and can't be decrypted here.  Re-enter it.  To move DBA Dash to another machine, enable config encryption before copying the config file.", ex);
            }
        }

        private static string UnprotectConfigKey(string value)
        {
            const string notAvailable = "The secret was encrypted with a key that isn't available.  If the config was copied from another machine, re-enter it (to move DBA Dash to another machine, enable config encryption before copying the config file).  For an encrypted config, it can only be decrypted as part of the encrypted config file it was saved in.";
            var payload = value[ConfigKeyPrefix.Length..];
            var idx = payload.IndexOf(':');
            if (idx < 0 || !KeyRing.TryGetValue(payload[..idx], out var key))
            {
                throw new SecretUnprotectException(notAvailable);
            }
            try
            {
                return key.DecryptSecret(payload[(idx + 1)..]);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                throw new SecretUnprotectException(notAvailable, ex);
            }
        }
    }

    /// <summary>
    /// Keys derived from the config encryption password: one encrypts the config file, the other the secrets inside it.
    /// PBKDF2 is slow by design so derived keys are cached for the process.
    /// </summary>
    public sealed class ConfigKey
    {
        public const int DefaultIterations = 600_000;
        public const int MinIterations = 10_000;
        /// <summary>Length of <see cref="Id"/> in hex characters</summary>
        public const int IdLength = 8;
        public const int MaxIterations = 10_000_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        /// <summary>The shortest value <see cref="EncryptSecret"/> produces (an empty string): nonce + tag</summary>
        internal const int MinEncryptedLength = NonceSize + TagSize;

        private static readonly ConcurrentDictionary<string, ConfigKey> DerivedKeys = new();

        private readonly byte[] configKey;
        private readonly byte[] secretKey;
        private readonly byte[] passwordCheck;

        public byte[] Salt { get; }
        public int Iterations { get; }

        /// <summary>Identifies the key in ¬=k1: values so a value encrypted with a different password is re-protected rather than misread</summary>
        public string Id { get; }

        private ConfigKey(string password, byte[] salt, int iterations) : this(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, KeySize))
        {
            Salt = salt;
            Iterations = iterations;
            passwordCheck = PasswordCheck(password, salt);
        }

        private ConfigKey(byte[] master)
        {
            configKey = HKDF.Expand(HashAlgorithmName.SHA256, master, KeySize, "DBADash config"u8.ToArray());
            secretKey = HKDF.Expand(HashAlgorithmName.SHA256, master, KeySize, "DBADash secrets"u8.ToArray());
            Id = Convert.ToHexString(HKDF.Expand(HashAlgorithmName.SHA256, master, IdLength / 2, "DBADash key id"u8.ToArray())).ToLowerInvariant();
            CryptographicOperations.ZeroMemory(master);
        }

        /// <summary>
        /// Create a random machine key for configs that aren't encrypted.  The key is stored in the config protected with DPAPI LocalMachine.
        /// Protecting each secret with DPAPI directly would work too, but each DPAPI blob is ~300 characters which makes connection strings unwieldy.
        /// </summary>
        /// <param name="protectedKey">The key protected with DPAPI to store in the config</param>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static ConfigKey CreateMachineKey(out string protectedKey)
        {
            var material = RandomNumberGenerator.GetBytes(KeySize);
            protectedKey = Convert.ToBase64String(material).ToMachineProtectedConfigValue();
            return new ConfigKey(material);
        }

        /// <summary>
        /// Load a machine key created with <see cref="CreateMachineKey"/>
        /// </summary>
        /// <exception cref="CryptographicException">The key was protected on a different machine</exception>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static ConfigKey LoadMachineKey(string protectedKey) => new(Convert.FromBase64String(protectedKey.FromMachineProtectedConfigValue()));

        // In-memory only: lets us tell if the password changed without running PBKDF2 again
        private static byte[] PasswordCheck(string password, byte[] salt) => HMACSHA256.HashData(salt, Encoding.UTF8.GetBytes(password));

        public bool IsForPassword(string password) =>
            password != null && passwordCheck != null && CryptographicOperations.FixedTimeEquals(passwordCheck, PasswordCheck(password, Salt));

        /// <summary>
        /// Derive the key for an existing config file
        /// </summary>
        public static ConfigKey Derive(string password, byte[] salt, int iterations)
        {
            ArgumentNullException.ThrowIfNull(password);
            var cacheKey = Convert.ToBase64String(salt) + "|" + iterations;
            if (DerivedKeys.TryGetValue(cacheKey, out var cached) && cached.IsForPassword(password))
            {
                return cached;
            }
            var key = new ConfigKey(password, salt, iterations);
            DerivedKeys[cacheKey] = key;
            return key;
        }

        /// <summary>
        /// Create a key with a new random salt
        /// </summary>
        public static ConfigKey Create(string password) => Derive(password, RandomNumberGenerator.GetBytes(SaltSize), DefaultIterations);

        /// <summary>
        /// Reuse <paramref name="existing"/> if it was derived from <paramref name="password"/> so the salt and the protected secrets stay the same between saves.  Otherwise create a new key.
        /// </summary>
        public static ConfigKey ForPassword(string password, ConfigKey existing)
        {
            ArgumentNullException.ThrowIfNull(password);
            return existing != null && existing.IsForPassword(password) ? existing : Create(password);
        }

        internal string EncryptConfig(string value) => Encrypt(configKey, value);

        internal string DecryptConfig(string value) => Decrypt(configKey, value);

        internal string EncryptSecret(string value) => Encrypt(secretKey, value);

        internal string DecryptSecret(string value) => Decrypt(secretKey, value);

        // Output: base64(nonce | tag | ciphertext)
        private static string Encrypt(byte[] key, string value)
        {
            var plain = Encoding.UTF8.GetBytes(value);
            var output = new byte[NonceSize + TagSize + plain.Length];
            var nonce = output.AsSpan(0, NonceSize);
            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plain, output.AsSpan(NonceSize + TagSize), output.AsSpan(NonceSize, TagSize));
            return Convert.ToBase64String(output);
        }

        private static string Decrypt(byte[] key, string value)
        {
            var input = Convert.FromBase64String(value);
            if (input.Length < NonceSize + TagSize) throw new CryptographicException("Invalid encrypted value");
            var plain = new byte[input.Length - NonceSize - TagSize];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(input.AsSpan(0, NonceSize), input.AsSpan(NonceSize + TagSize), input.AsSpan(NonceSize, TagSize), plain);
            return Encoding.UTF8.GetString(plain);
        }
    }

    public class SecretUnprotectException : Exception
    {
        public SecretUnprotectException(string message) : base(message)
        {
        }

        public SecretUnprotectException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
