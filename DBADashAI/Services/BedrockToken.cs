using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;

namespace DBADashAI.Services
{
    /// <summary>
    /// A short-term Bedrock API key, made from AWS credentials - for running on an IAM role, or any
    /// other source of temporary credentials, rather than storing a long-term key.
    ///
    /// The key is what AWS's own aws-bedrock-token-generator libraries make (there is none for .NET):
    /// a SigV4 presigned request to Bedrock's CallWithBearerToken action, base64-encoded.  It is sent
    /// exactly like a long-term key, so nothing else about a Bedrock call changes.  It is valid for 12
    /// hours or until the credentials that signed it expire, whichever is sooner - and making one is
    /// only a signature, so a fresh one is made for every call rather than kept.
    /// </summary>
    internal static class BedrockToken
    {
        private const string Host = "bedrock.amazonaws.com";
        private const string Prefix = "bedrock-api-key-";
        private const int ExpiresSeconds = 43200;

        /// <summary>SHA-256 of an empty body: the presigned request has none.</summary>
        private const string EmptyPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        /// <summary>
        /// Credentials by profile ("" for the default chain), kept once found.  They refresh themselves
        /// as they near expiry, so holding on to them is what the SDK expects; a failure is not kept,
        /// so a role attached after the service started is picked up on the next call.
        /// </summary>
        private static readonly ConcurrentDictionary<string, AWSCredentials> Credentials = new(StringComparer.Ordinal);

        /// <summary>
        /// A key signed with the credentials from Bedrock:Profile, or from the default AWS chain when no
        /// profile is set: environment variables, the default profile, web identity (EKS), the
        /// container role (ECS) or the instance role (EC2).
        /// </summary>
        internal static async Task<string> CreateAsync(string? profile, string region)
        {
            var key = profile?.Trim() ?? string.Empty;
            if (!Credentials.TryGetValue(key, out var credentials))
            {
                credentials = key.Length > 0
                    ? DBADash.AWSTools.GetAWSCredentialsFromProfile(key)
                    : DefaultAWSCredentialsIdentityResolver.GetCredentials();
                Credentials.TryAdd(key, credentials);
            }

            var current = await credentials.GetCredentialsAsync();
            return Create(current.AccessKey, current.SecretKey, current.UseToken ? current.Token : null, region, DateTime.UtcNow);
        }

        internal static string Create(string accessKey, string secretKey, string? sessionToken, string region, DateTime utcNow)
        {
            var date = utcNow.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            var scope = $"{date}/{region}/bedrock/aws4_request";

            var amzDate = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);
            // In the order AWS's generator writes them, so the key is the same one it would make.  The
            // signature covers them sorted, which is the order that matters to Bedrock.
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("Action", "CallWithBearerToken"),
                new("X-Amz-Algorithm", "AWS4-HMAC-SHA256"),
                new("X-Amz-Credential", $"{accessKey}/{scope}"),
                new("X-Amz-Date", amzDate),
                new("X-Amz-Expires", ExpiresSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("X-Amz-SignedHeaders", "host")
            };
            if (!string.IsNullOrEmpty(sessionToken))
                parameters.Add(new("X-Amz-Security-Token", sessionToken));

            var encoded = parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}").ToList();
            var query = string.Join("&", encoded);
            var canonicalQuery = string.Join("&", encoded.Order(StringComparer.Ordinal));
            var canonicalRequest = $"POST\n/\n{canonicalQuery}\nhost:{Host}\n\nhost\n{EmptyPayloadHash}";
            var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}";

            var signingKey = Hmac(Hmac(Hmac(Hmac(Encoding.UTF8.GetBytes("AWS4" + secretKey), date), region), "bedrock"), "aws4_request");
            var signature = Hex(Hmac(signingKey, stringToSign));

            var presigned = $"{Host}/?{query}&X-Amz-Signature={signature}&Version=1";
            return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(presigned));

            static byte[] Hmac(byte[] key, string data) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
            static string Hex(byte[] bytes) => Convert.ToHexStringLower(bytes);
        }
    }
}
