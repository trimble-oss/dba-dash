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
using System.Data.SqlClient;
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
            [Option('s', "source", Required = true, Separator = '|', HelpText = "Connection string for Monitored SQL Instance(s).  Use pipe character (|) to specify multiple sources.")]
            public IEnumerable<string> Source { get; set; }

            [Option('d', "destination", Required = true, HelpText = "Destination Connection string for DBAChecks DB")]
            public string Destination { get; set; }

            [Option("nowmi", Required = false, Default = false, HelpText = "Don't use WMI to collect drive information")]
            public bool NoWMI { get; set; }
     
            [Option('p', "awsprofile", Required = false, HelpText = "AWS Profile")]
            public string AWSProfile { get; set; }

            [Option('i', "interval", Required = false, Default = -1, HelpText = "Interval (mins) to repeat tests.  Default = run once.")]
            public Int32 IntervalMins { get; set; }

            [Option( "performanceonly", Required = false, Default =false, HelpText = "Collect performance data")]
            public bool PerformanceOnly { get; set; }
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
               while (1 == 1)
               {
                   DateTime collectionTime = DateTime.Now;
                   Int32 i = 1;
                   foreach (string s in o.Source)
                   {
                       try
                       {
                           Console.WriteLine(String.Format("Processing Source {0} of {1}", i, o.Source.Count()));
                           if (s.StartsWith("s3://") || s.StartsWith("https://"))
                           {
                               Console.WriteLine("Import from S3: " + s);
                               var uri = new Amazon.S3.Util.AmazonS3Uri(s);
                               var s3Cli = getAWSClient(o.AWSProfile, uri);
                               var resp = s3Cli.ListObjects(uri.Bucket, (uri.Key + "/DBAChecks_").Replace("//", "/"));
                               foreach (var f in resp.S3Objects)
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
                           else if (System.IO.Directory.Exists(s))
                           {
                               Console.WriteLine("Import from Directory: " + s);
                               foreach (var f in Directory.EnumerateFiles(s, "DBAChecks*.json", SearchOption.TopDirectoryOnly))
                               {
                                   string json = File.ReadAllText(f);
                                   DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);
                                   importer.Update(o.Destination, ds);
                                   File.Delete(f);
                                   Console.WriteLine("Imported:" + f);
                               }
                           }
                           else
                           {
                               SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(s);
                               Console.WriteLine("Collect from Instance:" + builder.DataSource);
                               var collector = new DBCollector(s, o.NoWMI);
                               if (o.PerformanceOnly)
                               {
                                   collector.CollectPerformance();
                               }
                               else
                               {
                                   collector.CollectAll();
                               }
                           
                               var ds = collector.Data;
                               if (o.Destination.StartsWith("s3://") || o.Destination.StartsWith("https://"))
                               {
                                   Console.WriteLine("Upload to S3");
                                   var uri = new Amazon.S3.Util.AmazonS3Uri(o.Destination);
                                   var s3Cli = getAWSClient(o.AWSProfile, uri);
                                   var r = new Amazon.S3.Model.PutObjectRequest();
                                   string fileName = "DBAChecks_" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss") + Guid.NewGuid().ToString() + ".json";
                                   string filePath = Path.Combine(o.Destination, fileName);
                                   string json = JsonConvert.SerializeObject(collector.Data, Formatting.None);
                                   r.ContentBody = json;
                                   r.BucketName = uri.Bucket;
                                   r.Key = (uri.Key + "/" + fileName).Replace("//", "/");

                                   s3Cli.PutObject(r);



                               }
                               else if (System.IO.Directory.Exists(o.Destination))
                               {
                                   Console.WriteLine("Write to folder");
                                   string fileName = "DBAChecks_" + DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss") + Guid.NewGuid().ToString() + ".json";
                                   string filePath = Path.Combine(o.Destination, fileName);
                                   string json = JsonConvert.SerializeObject(collector.Data, Formatting.None);
                                   File.WriteAllText(filePath, json);
                               }
                               else
                               {
                                   Console.WriteLine("Update DBAChecks DB");
                                   importer.Update(o.Destination, collector.Data);

                               }
                           }
                           i += 1;
                       }
                       catch(Exception ex)
                       {
                           Console.WriteLine(ex.Message);
                       }
                   }
                   if (o.IntervalMins <= 0)
                   {
                       break;
                   }
                   else
                   {
                       Int32 sleepTime = ((Int32)o.IntervalMins * 60000) - Convert.ToInt32(DateTime.Now.Subtract(collectionTime).TotalMilliseconds);
                       if (sleepTime > 0)
                       {
                           Console.WriteLine("Sleep For:" + sleepTime.ToString() + "ms");
                           System.Threading.Thread.Sleep(sleepTime);
                       }
                       Console.WriteLine("Continue....");
                   }
               }
        

           });

        }


    }
}
