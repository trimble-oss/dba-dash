using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBADash
{
    /// <summary>How the service writes its log file.</summary>
    public enum ServiceLogFileFormat
    {
        /// <summary>serilog.json has no file sink - logging was configured some other way.</summary>
        None,

        /// <summary>A line of text per event, from an output template.</summary>
        Text,

        /// <summary>Serilog's compact JSON format (CLEF) - a JSON object per event, which the log viewer shows as a grid.</summary>
        Json
    }

    /// <summary>
    /// The service's logging configuration.  serilog.json ships with each release, and an upgrade unzips over it, so a
    /// change made to it doesn't last.  A user's own choice - the log format, or any other edit - goes in
    /// serilog.local.json instead, which no release includes: the service reads it in place of serilog.json when it
    /// exists.
    /// </summary>
    public static class ServiceLogFormat
    {
        public const string DefaultConfigFileName = "serilog.json";
        public const string LocalConfigFileName = "serilog.local.json";

        public const string JsonFormatter = "Serilog.Formatting.Compact.RenderedCompactJsonFormatter, Serilog.Formatting.Compact";

        public const string TextOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} <{ThreadId}>{NewLine}{Exception}";

        private const string TextExtension = ".txt";
        private const string JsonExtension = ".json";

        /// <summary>The extensions a file in each format can keep when the format is switched to it.</summary>
        private static readonly string[] TextExtensions = [TextExtension, ".log"];

        private static readonly string[] JsonExtensions = [JsonExtension, ".clef", ".jsonl"];

        /// <summary>The configuration file the service reads: the user's own if there is one, otherwise the release's.</summary>
        public static string ConfigFileName(string folder) =>
            File.Exists(Path.Combine(folder, LocalConfigFileName)) ? LocalConfigFileName : DefaultConfigFileName;

        /// <summary>The format the service writes its log file in, from the configuration it reads.</summary>
        public static ServiceLogFileFormat GetFormat(string folder) =>
            GetFormat(Read(Path.Combine(folder, ConfigFileName(folder))));

        public static ServiceLogFileFormat GetFormat(JObject config)
        {
            var sinks = FileSinkArgs(config).ToList();
            if (sinks.Count == 0) return ServiceLogFileFormat.None;
            return sinks.Any(args => args["formatter"] != null) ? ServiceLogFileFormat.Json : ServiceLogFileFormat.Text;
        }

        /// <summary>
        /// Has the service write its log in <paramref name="format"/> from its next start.  The choice is kept in
        /// serilog.local.json, along with the user's other edits.  Where that leaves it the same as the release's
        /// serilog.json, the local file is removed, so the release's configuration is used and later ones are picked up.
        /// </summary>
        public static void SetFormat(string folder, ServiceLogFileFormat format)
        {
            var defaultPath = Path.Combine(folder, DefaultConfigFileName);
            var localPath = Path.Combine(folder, LocalConfigFileName);
            var config = Read(Path.Combine(folder, ConfigFileName(folder)));
            SetFormat(config, format);

            if (File.Exists(defaultPath) && JToken.DeepEquals(config, Read(defaultPath)))
            {
                File.Delete(localPath);
            }
            else
            {
                File.WriteAllText(localPath, config.ToString(Formatting.Indented));
            }
        }

        /// <summary>Changes each file sink in <paramref name="config"/> to <paramref name="format"/>, and its file extension to match.</summary>
        public static void SetFormat(JObject config, ServiceLogFileFormat format)
        {
            if (format == ServiceLogFileFormat.None) throw new ArgumentException("Choose Text or Json", nameof(format));

            var sinks = FileSinkArgs(config).ToList();
            if (sinks.Count == 0) throw new InvalidOperationException("The logging configuration has no file sink to change.");

            foreach (var args in sinks)
            {
                if (format == ServiceLogFileFormat.Json)
                {
                    args.Remove("outputTemplate");
                    args["formatter"] = JsonFormatter;
                }
                else
                {
                    args.Remove("formatter");
                    args["outputTemplate"] = TextOutputTemplate;
                }

                if (args["path"] is JValue { Type: JTokenType.String } path)
                {
                    path.Value = ChangeExtension((string)path.Value, format);
                }
            }
        }

        /// <summary>
        /// An extension that suits <paramref name="format"/> is kept - the user may have chosen .log or .clef.  Any
        /// other becomes .txt or .json, so the new format never goes on the end of a file in the old one: a rolling
        /// file restarted within the same period is appended to.
        /// </summary>
        private static string ChangeExtension(string path, ServiceLogFileFormat format)
        {
            var current = Path.GetExtension(path);
            var suits = format == ServiceLogFileFormat.Json ? JsonExtensions : TextExtensions;
            if (suits.Contains(current, StringComparer.OrdinalIgnoreCase)) return path;

            var extension = format == ServiceLogFileFormat.Json ? JsonExtension : TextExtension;
            return string.IsNullOrEmpty(current) ? path + extension : path[..^current.Length] + extension;
        }

        /// <summary>The Args of each File sink, at the top level of WriteTo or inside a wrapper such as Async.</summary>
        private static IEnumerable<JObject> FileSinkArgs(JObject config) =>
            config.SelectToken("Serilog.WriteTo") is JContainer writeTo
                ? writeTo.DescendantsAndSelf().OfType<JObject>()
                    .Where(sink => sink["Name"] is JValue { Type: JTokenType.String } name &&
                                   ((string)name.Value).Equals("File", StringComparison.OrdinalIgnoreCase) &&
                                   sink["Args"] is JObject)
                    .Select(sink => (JObject)sink["Args"])
                : [];

        private static JObject Read(string path) => JObject.Parse(File.ReadAllText(path));
    }
}
