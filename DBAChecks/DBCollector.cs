using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Management;

namespace DBAChecks
{
    class DBCollector
    {
        public DataSet Data;
        string _connectionString;
        private DataTable dtErrors;

        public DBCollector(string connectionString)
        {
                     startup(connectionString, null);

        }

        private void logError(string errorSource,string errorMessage)
        {
            Console.WriteLine(errorSource + " : " + errorMessage);
            var rError = dtErrors.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = errorMessage;
            dtErrors.Rows.Add(rError);
        }
        

        private void startup(string connectionString, string connectionID){
            _connectionString = connectionString;
            Data = new DataSet("DBAChecks");
            dtErrors = new DataTable("Errors");
            dtErrors.Columns.Add("ErrorMessage");
            dtErrors.Columns.Add("ErrorSource");
            Data.Tables.Add(dtErrors);
            GetInstance(connectionID);
        }

        public DBCollector(string connectionString, string connectionID )
        {
            startup(connectionString, connectionID);

        }

     

        public void GetInstance(string connectionID)
        {
             var dt = getDT("DBAChecks", "SELECT @@SERVERNAME as Instance,GETUTCDATE() As SnapshotDateUTC");
            dt.Columns.Add("DBAChecksVersion", typeof(Int32));
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Rows[0]["DBAChecksVersion"] = 1;
            if (connectionID == null)
            {
                dt.Rows[0]["ConnectionID"] = dt.Rows[0]["Instance"];
            }
            else
            {
                dt.Rows[0]["ConnectionID"] = connectionID;
            }
            Data.Tables.Add(dt);


        }

        public void CollectAll()
        {
            CollectProperies();
            CollectDatabases();
            CollectHADRDB();
            CollectConfiguration();
            CollectDrives();
            CollectFiles();
            CollectBackups();
            CollectAgentJobSummary();
            CollectLogShipping();
            CollectRegistryProperties();
        }

        public void CollectHADRDB()
        {
            try
            {
                addDT("HADRDB", DBAChecks.Properties.Resources.SQLHADRDB);
            }
            catch(Exception ex)
            {
                logError("HADRDB", ex.Message);
            }
        }

        public void CollectFiles()
        {
            try
            {
                addDT("Files", DBAChecks.Properties.Resources.SQLFiles);
            }
            catch (Exception ex)
            {
                logError("Collect Files", ex.Message);
            }
        }

        public void CollectProperies()
        {
            try
            {
                addDT("Properties", DBAChecks.Properties.Resources.SQLProperties);
            }
            catch(Exception ex)
            {
                logError("Collect Properties", ex.Message);
            }
        }

        public void CollectRegistryProperties()
        {
            try
            {
                addDT("RegistryProperties", DBAChecks.Properties.Resources.RegistryProperties);
            }
            catch(Exception ex)
            {
                logError("Collect Registry Properties", ex.Message);
            }
        }

        public void CollectDatabases()
        {
            try
            {
                addDT("Databases", Properties.Resources.SQLDatabases);
            }
            catch(Exception ex)
            {
                logError("Collect Databases", ex.Message);
            }
        }

        public void CollectConfiguration()
        {
            try { 
                addDT("Configuration", DBAChecks.Properties.Resources.SQLConfigurations);
            }
            catch(Exception ex)
            {
                logError("Collect Configuration", ex.Message);
            }
 
            //addDT(dt, DBAChecks.Properties.Resources.SQLProperties);
        }

        public void CollectAgentJobSummary()
        {
            try
            {
                addDT("AgentJobs", DBAChecks.Properties.Resources.SQLAgentJobSummary);
            }
            catch(Exception ex)
            {
                logError("Collect Agent Jobs", ex.Message);
            }
        }

        public void CollectBackups()
        {
            try
            {
                addDT("Backups", DBAChecks.Properties.Resources.SQLBackups);
            }
            catch(Exception ex)
            {
                logError("Collect Backups", ex.Message);
            }
        }

        public void CollectLogShipping()
        {
            try
            {
                addDT("LogRestores", DBAChecks.Properties.Resources.SQLLogShipping);
            }
            catch(Exception ex)
            {
                logError("Collect Log Restores", ex.Message);
            }
        }

        public DataTable getDT (string tableName, string SQL)
        {
            SqlConnection cn = new SqlConnection(_connectionString);
            using (cn)
            {
                cn.Open();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(SQL, cn);
                da.Fill(dt);
                dt.TableName = tableName;
                return dt;


            }
        }

        public void addDT(string tableName,string sql)
        {
            if (!Data.Tables.Contains(tableName))
            {
                
                    Data.Tables.Add(getDT(tableName, sql));
                                
            }
        }

        public void addDT(DataTable dt, string sql)
        {
            if (!Data.Tables.Contains(dt.TableName))
            {
                SqlConnection cn = new SqlConnection(_connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(sql, cn);
                    da.Fill(dt);
                    Data.Tables.Add(dt);

                }
            }
        }

        public void CollectDrivesSQL()
        {
            try
            {
                addDT("Drives", Properties.Resources.SQLDrives);
            }
            catch(Exception ex)
            {
                logError("Collect Drives(SQL)", ex.Message);
            }
        }

        public void CollectDrives()
        {
            try
            {
                CollectDrivesWMI();
            }
            catch(Exception ex)
            {
                logError("Collect Drives","Error collecting drives via WMI.  Drive info will be collected from SQL, but might be incomplete");
                CollectDrivesSQL();
            }
        }

        public void CollectDrivesWMI()
        {
            try {
                if (!Data.Tables.Contains("Drives")) {
                    DataTable drives = new DataTable("Drives");
                    drives.Columns.Add("Name", typeof(string));
                    drives.Columns.Add("Capacity", typeof(Int64));
                    drives.Columns.Add("FreeSpace", typeof(Int64));
                    drives.Columns.Add("Label", typeof(string));
                    string computerName = (string)Data.Tables["Properties"].Rows[0]["ComputerNamePhysicalNetBIOS"];

                    ManagementPath path = new ManagementPath()
                    {
                        NamespacePath = @"root\cimv2",
                        Server = computerName
                    };
                    ManagementScope scope = new ManagementScope(path);
                    //string condition = "DriveLetter = 'C:'";
                    string[] selectedProperties = new string[] { "FreeSpace", "Name", "Capacity", "Caption", "Label" };
                    SelectQuery query = new SelectQuery("Win32_Volume", "DriveType=3 AND DriveLetter IS NOT NULL", selectedProperties);

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        foreach (ManagementObject volume in results)
                        {


                            if (volume != null)
                            {
                                var rDrive = drives.NewRow();
                                rDrive["FreeSpace"] = (UInt64)volume.GetPropertyValue("FreeSpace");
                                rDrive["Name"] = (string)volume.GetPropertyValue("Name");
                                rDrive["Capacity"] = (UInt64)volume.GetPropertyValue("Capacity");
                                rDrive["Label"] = (string)volume.GetPropertyValue("Label");
                                drives.Rows.Add(rDrive);
                                // Use freeSpace here...
                            }
                        }
                    }
                    Data.Tables.Add(drives);

                }
            }
            catch(Exception ex)
            {
                logError("Collect Drives (WMI)", ex.Message);
                throw ex;
            }
         }


    }
}
