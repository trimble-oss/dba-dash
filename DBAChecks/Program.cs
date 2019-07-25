using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using CommandLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace DBAChecks
{
    class Program
    {
        public class Options
        {
            [Option('s', "source", Required = true, HelpText = "Connection string for Monitored SQL Instance")]
            public string Source{ get; set; }
            [Option('d', "destination", Required = true, HelpText = "Destination Connection string for DBAChecks DB")]
            public string Destination { get; set; }

            [Option("nowmi", Required = false, Default = false, HelpText = "Don't use WMI to collect drive information")]
            public bool NoWMI { get; set; }
     
            [Option('p', "awsprofile", Required = false, HelpText = "AWS Profile")]
            public string AWSProfile { get; set; }
        }

        private static AWSCredentials GetAWSCredentialsFromProfile(string profileName)
        {
            var credentialProfileStoreChain = new CredentialProfileStoreChain();
            AWSCredentials defaultCredentials;
            if (credentialProfileStoreChain.TryGetAWSCredentials(profileName, out defaultCredentials))
                return defaultCredentials;
            else
                throw new AmazonClientException("Unable to find profile in CredentialProfileStoreChain.");
        }

        private static Amazon.S3.AmazonS3Client getAWSClient(string profile, Amazon.S3.Util.AmazonS3Uri uri)
        {
            AWSCredentials cred = null;
            if (profile == null)
            {
                profile = "default";
            }
            try
            {
                cred = GetAWSCredentialsFromProfile(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
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



        static void Main(string[] args)
        {
            var importer = new DBImporter();
            Parser.Default.ParseArguments<Options>(args)
           .WithParsed<Options>(o =>
           {
           if (o.Source.StartsWith("s3://") || o.Source.StartsWith("https://"))
           {
               Console.WriteLine("Import from S3");
               var uri = new Amazon.S3.Util.AmazonS3Uri(o.Source);
               var s3Cli = getAWSClient(o.AWSProfile, uri);
               var resp = s3Cli.ListObjects(uri.Bucket, (uri.Key + "/DBAChecks_").Replace("//", "/"));
                foreach(var f in resp.S3Objects)
                   {
                       if (f.Key.EndsWith(".json"))
                       {           
                           using (GetObjectResponse response = s3Cli.GetObject(f.BucketName, f.Key))
                           using (Stream responseStream = response.ResponseStream)
                           using (StreamReader reader = new StreamReader(responseStream))
                           {
                              string json = reader.ReadToEnd();
                              DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);
                              importer.Update(o.Destination, ds);
                              s3Cli.DeleteObject(f.BucketName, f.Key);
                           }
                           Console.WriteLine("Imported:" + f.Key);
                       }
                   }    
               }
               else if (System.IO.Directory.Exists(o.Source))
               {
                   Console.WriteLine("Import from Directory");
                   foreach(var f in Directory.EnumerateFiles(o.Source,"DBAChecks*.json", SearchOption.TopDirectoryOnly))
                   {
                       string json= File.ReadAllText(f);
                       DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);
                       importer.Update(o.Destination, ds);
                       File.Delete(f);
                       Console.WriteLine("Imported:" + f);
                   }
               }
               else
               {
                   var collector = new DBCollector(o.Source,o.NoWMI);
                   collector.CollectAll();
                   var ds = collector.Data;
                   if (o.Destination.StartsWith("s3://") || o.Destination.StartsWith("https://"))
                   {

                       var uri = new Amazon.S3.Util.AmazonS3Uri(o.Destination);
                       var s3Cli = getAWSClient(o.AWSProfile, uri); 
                       var r = new Amazon.S3.Model.PutObjectRequest();
                        string fileName = "DBAChecks_" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss") + Guid.NewGuid().ToString() + ".json";
                        string filePath = Path.Combine(o.Destination, fileName);
                        string json = JsonConvert.SerializeObject(collector.Data, Formatting.None);
                        r.ContentBody = json;
                        r.BucketName = uri.Bucket;
                        r.Key =(uri.Key + "/" + fileName).Replace("//","/");
                          
                        s3Cli.PutObject(r);
                     
                       
     
                   }
                   else if (System.IO.Directory.Exists(o.Destination))
                   {
                       string fileName = "DBAChecks_" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss") + Guid.NewGuid().ToString() + ".json";
                       string filePath = Path.Combine(o.Destination, fileName);
                       string json = JsonConvert.SerializeObject(collector.Data, Formatting.None);
                       File.WriteAllText(filePath, json);
                   }
                   else
                   {
                      
                       importer.Update(o.Destination, collector.Data);

                   }
               }

             
        

           });

        }


    }
}
