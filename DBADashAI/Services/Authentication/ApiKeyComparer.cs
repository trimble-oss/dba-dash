using System.Security.Cryptography;
using System.Text;

namespace DBADashAI.Services.Authentication;

/// <summary>
/// Constant-time API key comparison that is safe against both early-exit timing
/// attacks (length leaks) and character-by-character timing attacks.
///
/// <para>
/// <see cref="CryptographicOperations.FixedTimeEquals"/> returns <c>false</c>
/// immediately when the two spans differ in length, which leaks whether the
/// attacker's guess has the same byte length as a stored key.  To close that
/// gap both values are first run through HMAC-SHA256 with a per-process random
/// key.  The resulting digests are always 32 bytes, so the subsequent
/// <see cref="CryptographicOperations.FixedTimeEquals"/> call is unconditionally
/// constant-time regardless of the original key lengths.
/// </para>
/// </summary>
internal static class ApiKeyComparer
{
    // One random key per process lifetime.  It never leaves this class, so an
    // attacker who can read memory already has the stored keys and wins anyway.
    private static readonly byte[] _hmacKey = RandomNumberGenerator.GetBytes(32);

    /// <summary>
    /// Returns <c>true</c> if <paramref name="providedKey"/> matches any entry in
    /// <paramref name="validKeys"/> using a constant-time comparison.
    /// All entries are always evaluated — no short-circuit on first match —
    /// so the number of stored keys is not revealed by response timing.
    /// </summary>
    public static bool TimingSafeContains(string[] validKeys, string providedKey)
    {
        // HMAC the provided key once; reuse the digest for every stored-key comparison.
        var providedDigest = Hmac(providedKey);

        var matched = false;
        foreach (var key in validKeys)
        {
            var storedDigest = Hmac(key);
            // Both digests are 32 bytes — FixedTimeEquals is now truly constant-time.
            if (CryptographicOperations.FixedTimeEquals(providedDigest, storedDigest))
                matched = true;  // do NOT break — always iterate all keys
        }
        return matched;
    }

    private static byte[] Hmac(string value)
        => HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(value));
}
