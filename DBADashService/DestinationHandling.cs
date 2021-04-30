using DBADash;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using static DBADash.DBADashConnection;

namespace DBADashService
{
    public class DestinationHandling
    {

        public static void WriteAllDestinations(DataSet ds,DBADashSource src,string fileName)
        {
            List<Exception> exceptions = new List<Exception>();
            foreach (var d in SchedulerServiceConfig.Config.AllDestinations)
            {
                try
                {
                    ScheduleService.InfoLogger("Write to destination:" + d.ConnectionForPrint + " from " + src.SourceConnection.ConnectionForPrint);
                    Write(ds, d, fileName);
                }
                catch(Exception ex)
                {
                    exceptions.Add(ex);
                    DBADashService.ScheduleService.ErrorLogger(ex,"Error writing to destination:" + d.ConnectionForPrint + " from " + src.SourceConnection.ConnectionForPrint);
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        public static void Write(DataSet ds, DBADashConnection d,string fileName)
        {
            switch (d.Type)
            {
                case ConnectionType.AWSS3:
                    WriteS3(ds, d.ConnectionString, fileName);
                    break;
                case ConnectionType.Directory:
                    WriteFolder(ds, d.ConnectionString, fileName);
                    break;
                case ConnectionType.SQL:
                    WriteDB(ds, d.ConnectionString);
                    break;
            }

        }

  
        public static void WriteS3(DataSet ds, string destination, string fileName)
        {
            var uri = new Amazon.S3.Util.AmazonS3Uri(destination);
            var s3Cli = AWSTools.GetAWSClient(SchedulerServiceConfig.Config.AWSProfile, SchedulerServiceConfig.Config.AccessKey, SchedulerServiceConfig.Config.GetSecretKey(), uri);
            var r = new Amazon.S3.Model.PutObjectRequest();

            if (System.IO.Path.GetExtension(fileName) == ".bin")
            {
                DataSetSerialization.SetDateTimeKind(ds); // Required for binary dataset serialization to prevent timezone conversion
                ds.RemotingFormat = SerializationFormat.Binary;
                BinaryFormatter fmt = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                fmt.Serialize(ms, ds);
                r.InputStream = ms;
            }
            else
            {
                string json = DataSetSerialization.SerializeDS(ds);
                r.ContentBody = json;
                r.BucketName = uri.Bucket;
            }
            r.BucketName = uri.Bucket;
            r.Key = (uri.Key + "/" + fileName).Replace("//", "/");
            s3Cli.PutObject(r);

        }

        public static void WriteFolder(DataSet ds, string destination, string fileName)
        {
            if (System.IO.Directory.Exists(destination))
            {
                string filePath = Path.Combine(destination, fileName);
                if (System.IO.Path.GetExtension(fileName) == ".bin")
                {
                    DataSetSerialization.SetDateTimeKind(ds); // Required for binary dataset serialization to prevent timezone conversion
                    ds.RemotingFormat = SerializationFormat.Binary;
                    BinaryFormatter fmt = new BinaryFormatter();
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        fmt.Serialize(fs, ds);
                    }
                }
                else
                {
                    string json = DataSetSerialization.SerializeDS(ds);
                    File.WriteAllText(filePath, json);
                }
            }
            else
            {
                ScheduleService.InfoLogger("Destination Folder doesn't exist");
            }
        }

        public static void WriteDB(DataSet ds, string destination)
        {
            var importer = new DBImporter(ds, destination);
            // Wait until we can connect to the repository DB.  If it's down, wait for it to become available.
            Policy.Handle<Exception>()
              .WaitAndRetryForever(retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timespan) =>
                    {
                        ScheduleService.ErrorLogger(exception, "Connect to repository DB");
                    }
                    )
              .Execute(() => importer.TestConnection());
           
            importer.Update();
        }
    }
}
