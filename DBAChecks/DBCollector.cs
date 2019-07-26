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
        private bool noWMI;
        public Int32 CPUCollectionPeriod=60;

        public DBCollector(string connectionString, bool noWMI)
        {
                  startup(connectionString, null);
                 this.noWMI = noWMI;
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
            dtErrors.Columns.Add("ErrorSource");
            dtErrors.Columns.Add("ErrorMessage");
           
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
            dt.Columns.Add("AgentHostName", typeof(string));
            dt.Rows[0]["DBAChecksVersion"] = 1;
            dt.Rows[0]["AgentHostName"] = Environment.MachineName;
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
            CollectServerExtraProperties();
            CollectDBConfig();
            CollectCorruption();
            CollectOSInfo();
            CollectTraceFlags();
        }



        public void CollectPerformance()
        {
            try
            {
                addDT("ProcStats", Properties.Resources.SQLStoredProcPerformance);
            }
            catch(Exception ex)
            {
                logError("ProcStats", ex.Message);
            }
            try
            {
                addDT("FunctionStats", Properties.Resources.SQLFunctionPerformance);
            }
            catch (Exception ex)
            {
                logError("FunctionStats", ex.Message);
            }
            try
            {
                SqlParameter pTop = new SqlParameter("TOP", CPUCollectionPeriod);
                addDT("CPU", Properties.Resources.SQLCPU, new SqlParameter[] { pTop });
            }
            catch (Exception ex)
            {
                logError("CPU", ex.Message);
            }
        }

        public void CollectTraceFlags()
        {
            try
            {
                addDT("TraceFlags", DBAChecks.Properties.Resources.SQLTraceFlags);
            }
            catch (Exception ex)
            {
                logError("TraceFlags", ex.Message);
            }
        }

        public void CollectOSInfo()
        {
            try
            {
                addDT("OSInfo", DBAChecks.Properties.Resources.SQLOSInfo);
            }
            catch(Exception ex)
            {
                logError("OSInfo", ex.Message);
            }
        }

        public void CollectCorruption()
        {
            try
            {
                addDT("Corruption", DBAChecks.Properties.Resources.SQLCorruption);
            }
            catch (Exception ex)
            {
                logError("CollectCorruption", ex.Message);
            }
        }

        public void CollectDBConfig()
        {
            try
            {
                addDT("DBConfig", DBAChecks.Properties.Resources.SQLDBConfig);
            }
            catch (Exception ex)
            {
                logError("DBConfig", ex.Message);
            }
        }

        public void CollectHADRDB()
        {
            try
            {
                addDT("DatabasesHADR", DBAChecks.Properties.Resources.SQLHADRDB);
            }
            catch(Exception ex)
            {
                logError("DatabasesHADR", ex.Message);
            }
        }

        public void CollectFiles()
        {
            try
            {
                addDT("DBFiles", DBAChecks.Properties.Resources.SQLFiles);
            }
            catch (Exception ex)
            {
                logError("DBFiles", ex.Message);
            }
        }

        public void CollectProperies()
        {
            try
            {
                addDT("ServerProperties", DBAChecks.Properties.Resources.SQLProperties);
            }
            catch(Exception ex)
            {
                logError("ServerProperties", ex.Message);
            }
        }

        public void CollectServerExtraProperties()
        {
            try
            {
                addDT("ServerExtraProperties", DBAChecks.Properties.Resources.SQLExtraProperties);
            }
            catch(Exception ex)
            {
                logError("ServerExtraProperties", ex.Message);
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
                addDT("SysConfig", DBAChecks.Properties.Resources.SQLConfigurations);
            }
            catch(Exception ex)
            {
                logError("SysConfig", ex.Message);
            }
 
 
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

 

            public DataTable getDT (string tableName, string SQL, SqlParameter[] param=null)
        {
            SqlConnection cn = new SqlConnection(_connectionString);
            using (cn)
            {
                cn.Open();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(SQL, cn);
                if (param != null)
                {
                    da.SelectCommand.Parameters.AddRange(param);
                }
                da.Fill(dt);
                dt.TableName = tableName;
                return dt;


            }
        }

        public void addDT(string tableName,string sql,SqlParameter[] param=null)
        {
            if (!Data.Tables.Contains(tableName))
            {
                
                    Data.Tables.Add(getDT(tableName, sql,param));
                                
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
            if (noWMI)
            {
                CollectDrivesSQL();
            }
            else
            {
                try
                {
                    CollectDrivesWMI();
                }
                catch (Exception ex)
                {
                    logError("Collect Drives", "Error collecting drives via WMI.  Drive info will be collected from SQL, but might be incomplete.  Use --nowmi switch to collect through SQL as default.");
                    CollectDrivesSQL();
                }
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
                    string computerName = (string)Data.Tables["ServerProperties"].Rows[0]["ComputerNamePhysicalNetBIOS"];

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
