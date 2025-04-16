using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    internal class DatabaseInfo
    {
        public int InstanceID { get; }
        public int DatabaseID { get; }
        public string InstanceName { get; }
        public string DatabaseName { get; }

        public DatabaseInfo(int instanceID, int databaseID, string instanceName, string databaseName)
        {
            InstanceID = instanceID;
            DatabaseID = databaseID;
            InstanceName = instanceName;
            DatabaseName = databaseName;
        }

        public override string ToString()
        {
            return $"({InstanceID}, {DatabaseID}, {InstanceName}, {DatabaseName})";
        }
    }
}