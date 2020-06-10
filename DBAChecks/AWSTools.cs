using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using System;

namespace DBAChecks
{
    public class AWSTools
    {
        public static AWSCredentials GetAWSCredentialsFromProfile(string profileName)
        {
            var credentialProfileStoreChain = new CredentialProfileStoreChain();
            AWSCredentials defaultCredentials;
            if (credentialProfileStoreChain.TryGetAWSCredentials(profileName, out defaultCredentials))
                return defaultCredentials;
            else
                throw new AmazonClientException("Unable to find profile in CredentialProfileStoreChain.");
        }

        public static Amazon.S3.AmazonS3Client GetAWSClient(string profile, string accessKey, string secretKey, Amazon.S3.Util.AmazonS3Uri uri)
        {
            AWSCredentials cred = null;
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
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
            else
            {
                cred = new InstanceProfileAWSCredentials();
            }

            Amazon.S3.AmazonS3Client cli = new AmazonS3Client(cred, RegionEndpoint.EUWest2);


            RegionEndpoint AWSRegion;
            if (uri.Region != null)
            {
                AWSRegion = uri.Region;
            }
            else
            {
                AWSRegion = RegionEndpoint.GetBySystemName(cli.GetBucketLocation(uri.Bucket).Location);
            }
            var s3Cli = new AmazonS3Client(cred, AWSRegion);
            return s3Cli;


        }
    }
}
