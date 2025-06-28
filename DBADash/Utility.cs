using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DBADash
{
    internal class Utility
    {
        public static string GetResourceString(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourcePath) ?? throw new Exception($"Resource {resourcePath} not found.");
            using StreamReader reader = new(stream);

            return reader.ReadToEnd();
        }
    }
}