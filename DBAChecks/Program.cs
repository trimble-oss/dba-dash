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
        }

            static void Main(string[] args)
        {
            var importer = new DBImporter();
            Parser.Default.ParseArguments<Options>(args)
           .WithParsed<Options>(o =>
           {
               if (System.IO.Directory.Exists(o.Source))
               {
                   foreach(var f in Directory.EnumerateFiles(o.Source,"DBAChecks*.json", SearchOption.TopDirectoryOnly))
                   {
                        string json= File.ReadAllText(f);
                        DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);
                       importer.Update(o.Destination, ds);
                       File.Delete(f);
                   }
               }
               else
               {
                   var collector = new DBCollector(o.Source);
                   collector.CollectAll();
                   var ds = collector.Data;
                   if (System.IO.Directory.Exists(o.Destination))
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
