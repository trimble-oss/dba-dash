using DBADash;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using static DBADash.DBADashConnection;
using Serilog;
using SerilogTimings;
using System.Threading.Tasks;

namespace DBADashService
{
    public class DestinationHandling
    {

        public static async Task WriteAllDestinations(DataSet ds,DBADashSource src,string fileName)
        {
            List<Exception> exceptions = new();
            foreach (var d in SchedulerServiceConfig.Config.AllDestinations)
            {
                try
                {
                    using (var op = Operation.Begin("Write to destination {destination} from {source}", d.ConnectionForPrint, src.SourceConnection.ConnectionForPrint))
                    {
                        await Write(ds, d, fileName);
                        op.Complete();
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Error writing to destination {destination} from {source}", d.ConnectionForPrint, src.SourceConnection.ConnectionForPrint);
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        public static async Task Write(DataSet ds, DBADashConnection d,string fileName)
        {
            switch (d.Type)
            {
                case ConnectionType.AWSS3:
                    await WriteS3(ds, d.ConnectionString, fileName);
                    break;
                case ConnectionType.Directory:
                    WriteFolder(ds, d.ConnectionString, fileName);
                    break;
                case ConnectionType.SQL:
                    WriteDB(ds, d.ConnectionString);
                    break;
            }

        }

  
        public static async Task WriteS3(DataSet ds, string destination, string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName);
            if (extension != ".xml")
            {
                fileName += ".xml";
            }
            DataSetSerialization.SetDateTimeKind(ds); // Required to prevent timezone conversion

            var uri = new Amazon.S3.Util.AmazonS3Uri(destination);
 
            using (var s3Cli = AWSTools.GetAWSClient(SchedulerServiceConfig.Config.AWSProfile, SchedulerServiceConfig.Config.AccessKey, SchedulerServiceConfig.Config.GetSecretKey(), uri))
            {

                var r = new Amazon.S3.Model.PutObjectRequest()
                {
                    BucketName = uri.Bucket,
                    Key = (uri.Key + "/" + fileName).Replace("//", "/")
                };
            
                using (var ms = new MemoryStream())
                {
                    ds.WriteXml(ms, XmlWriteMode.WriteSchema);
                    r.InputStream = ms;
                    await s3Cli.PutObjectAsync(r);
                }
            }

        }

        public static void WriteFolder(DataSet ds, string destination, string fileName)
        {
            if (System.IO.Directory.Exists(destination))
            {
                string filePath = Path.Combine(destination, fileName);
                string extension = System.IO.Path.GetExtension(fileName);
                if (extension == ".xml")
                {
                    using (FileStream fs = new(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        DataSetSerialization.SetDateTimeKind(ds); // Required to prevent timezone conversion
                        ds.WriteXml(fs, XmlWriteMode.WriteSchema);
                    }
                }
                else
                {
                    throw new Exception("Invalid extension");
                }
            }
            else
            {
               Log.Error("Destination Folder doesn't exist {folder}",destination);
            }
        }

        public static void WriteDB(DataSet ds, string destination)
        {
            var importAgent = DBADashAgent.GetCurrent(SchedulerServiceConfig.Config.ServiceName);
            var importer = new DBImporter(ds, destination, importAgent);
            // Wait until we can connect to the repository DB.  If it's down, wait for it to become available.
            Policy.Handle<Exception>()
              .WaitAndRetryForever(retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timespan) =>
                    {
                        Log.Error(exception, "Error connecting to repository database");
                    }
                    )
              .Execute(() => importer.TestConnection());
           
            importer.Update();
        }
    }
}
