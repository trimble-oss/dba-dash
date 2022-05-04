using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Runtime.Versioning;
using System.IO;
namespace DBADash
{

    [SupportedOSPlatform("windows")]
    public static class ServiceTools
    {

        public static bool IsServiceInstalledByPath(string ServicePath)
        {
            using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name,PathName from Win32_Service");

            var collection = searcher.Get().Cast<ManagementBaseObject>()
                .Where(service => ((string)service.GetPropertyValue("PathName")).Contains(ServicePath))
                .Select(service => (string)service.GetPropertyValue("Name"));

            return collection.Any();
        }

        public static bool IsServiceInstalledByPath()
        {
            return IsServiceInstalledByPath(ServicePath);
        }
        

        public static bool IsServiceInstalledByName(string ServiceName)
        {
            using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name from Win32_Service");

            var collection = searcher.Get().Cast<ManagementBaseObject>()
                .Where(service => (string)service.GetPropertyValue("Name")==ServiceName)
                .Select(service => (string)service.GetPropertyValue("Name"));

            return collection.Any();
        }

        public static string GetServiceNameFromPath(string ServicePath)
        {
            using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name,PathName from Win32_Service");

            var collection = searcher.Get().Cast<ManagementBaseObject>()
                .Where(service => ((string)service.GetPropertyValue("PathName")).Contains(ServicePath))
                .Select(service => (string)service.GetPropertyValue("Name"));
            
            return collection.FirstOrDefault(string.Empty);
            
        }
        
 
        public static string GetPathOfService(string ServiceName)
        {
            using var searcher = new ManagementObjectSearcher("root\\cimv2", "SELECT Name,PathName from Win32_Service");

            var collection = searcher.Get().Cast<ManagementBaseObject>()
                .Where(service => (string)service.GetPropertyValue("Name") == ServiceName)
                .Select(service => (string)service.GetPropertyValue("PathName"));

            return collection.FirstOrDefault(string.Empty);         

        }
        

        public static string GetServiceNameFromPath()
        {           
            return GetServiceNameFromPath(ServicePath);
        }

        public static string ServicePath= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DBADashService.exe");

    }
}
