using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Util;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
namespace DBADash
{
    public class AWSTools
    {
        private static readonly ConcurrentDictionary<string, RegionEndpoint> BucketRegionCache = new ConcurrentDictionary<string, RegionEndpoint>();

        public static AWSCredentials GetAWSCredentialsFromProfile(string profileName)
        {
            var credentialProfileStoreChain = new CredentialProfileStoreChain();
            if (credentialProfileStoreChain.TryGetAWSCredentials(profileName, out AWSCredentials defaultCredentials))
                return defaultCredentials;
            else
                throw new AmazonClientException("Unable to find profile in CredentialProfileStoreChain.");
        }

        public static AWSCredentials GetCredentials(string profile, string accessKey, string secretKey)
        {
            AWSCredentials cred;
            if (accessKey != null && secretKey != null && accessKey.Length > 0 && secretKey.Length > 0)
            {
                cred = new BasicAWSCredentials(accessKey, secretKey);
            }
            else if (profile is { Length: > 0 })
            {
                try
                {
                    cred = GetAWSCredentialsFromProfile(profile);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting AWS creds from profile {profile}", profile);
                    throw;
                }
            }
            else
            {
                cred = new InstanceProfileAWSCredentials();
            }
            return cred;
        }

        public static async Task<AmazonS3Client> GetAWSClientAsync(string profile, string accessKey, string secretKey, AmazonS3Uri uri)
        {
            var cred = GetCredentials(profile, accessKey, secretKey);
            if (!BucketRegionCache.TryGetValue(uri.Bucket, out var AWSRegion))
            {
                using var tempClient = new AmazonS3Client(cred, RegionEndpoint.EUWest2);
                AWSRegion = uri.Region ?? RegionEndpoint.GetBySystemName((await tempClient.GetBucketLocationAsync(uri.Bucket)).Location);
                BucketRegionCache.TryAdd(uri.Bucket, AWSRegion);
            }

            return new AmazonS3Client(cred, AWSRegion);
        }
    }
}
