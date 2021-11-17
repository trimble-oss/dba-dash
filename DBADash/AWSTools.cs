using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using System;
using Serilog;
namespace DBADash
{
    public class AWSTools
    {
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
            else if (profile != null && profile.Length > 0)
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

        public static Amazon.S3.AmazonS3Client GetAWSClient(string profile, string accessKey, string secretKey, Amazon.S3.Util.AmazonS3Uri uri)
        {
            AWSCredentials cred = GetCredentials(profile, accessKey, secretKey);

            using (Amazon.S3.AmazonS3Client cli = new AmazonS3Client(cred, RegionEndpoint.EUWest2))
            {

                RegionEndpoint AWSRegion;
                if (uri.Region != null)
                {
                    AWSRegion = uri.Region;
                }
                else
                {
                    AWSRegion = RegionEndpoint.GetBySystemName(cli.GetBucketLocationAsync(uri.Bucket).Result.Location);
                }
                var s3Cli = new AmazonS3Client(cred, AWSRegion);
                return s3Cli;
            }

        }
    }
}
