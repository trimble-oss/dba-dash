using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Management;
using System.Reflection;

namespace DBADash
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CollectionType
    {
        General,
        Performance,
        Infrequent,
        AgentJobs,
        Databases,
        DatabasesHADR,
        SysConfig,
        Drives,
        DBConfig,
        DBFiles,
        Corruption,
        OSInfo,
        TraceFlags,
        DriversWMI,
        CPU,
        BlockingSnapshot,
        IOStats,
        Waits,
        Backups,
        LogRestores,
        ServerProperties,
        ServerExtraProperties,
        OSLoadedModules,
        DBTuningOptions,
        AzureDBResourceStats,
        AzureDBServiceObjectives,
        AzureDBElasticPoolResourceStats,
        SlowQueries,
        LastGoodCheckDB,
        Alerts,
        ObjectExecutionStats,
        ServerPrincipals,
        ServerRoleMembers,
        ServerPermissions,
        DatabasePrincipals,
        DatabaseRoleMembers,
        DatabasePermissions,
        CustomChecks,
        PerformanceCounters,
        VLF,
        DatabaseMirroring
    }

    public enum HostPlatform
    {
        Linux,
        Windows
    }


    public class DBCollector
    {
        public DataSet Data;
        string _connectionString;
        private DataTable dtErrors;
        private bool noWMI;
        public Int32 PerformanceCollectionPeriodMins = 60;
        string computerName;
        Int64 editionId;
        readonly CollectionType[] azureCollectionTypes = new CollectionType[] { CollectionType.SlowQueries, CollectionType.AzureDBElasticPoolResourceStats, CollectionType.AzureDBServiceObjectives, CollectionType.AzureDBResourceStats, CollectionType.CPU, CollectionType.DBFiles, CollectionType.General, CollectionType.Performance, CollectionType.Databases, CollectionType.DBConfig, CollectionType.TraceFlags, CollectionType.ObjectExecutionStats, CollectionType.BlockingSnapshot, CollectionType.IOStats, CollectionType.Waits, CollectionType.ServerProperties, CollectionType.DBTuningOptions, CollectionType.SysConfig, CollectionType.DatabasePrincipals, CollectionType.DatabaseRoleMembers, CollectionType.DatabasePermissions, CollectionType.Infrequent, CollectionType.OSInfo,CollectionType.CustomChecks,CollectionType.PerformanceCounters,CollectionType.VLF };
        public Int64 SlowQueryThresholdMs = -1;
        public Int32 SlowQueryMaxMemoryKB { get; set; } = 4096;
        public bool UseDualEventSession { get; set; } = true;

        private bool IsAzure = false;
        private bool isAzureMasterDB = false;
        private string instanceName;
        string dbName;
        string productVersion;
        public Int32 RetryCount=1;
        public Int32 RetryInterval = 30;
        private HostPlatform platform;


        public bool IsXESupported()
        {
            return DBADashConnection.IsXESupported(productVersion);
        }

        public DBCollector(string connectionString, bool noWMI)
        {
            this.noWMI = noWMI;
            startup(connectionString, null);
        }

        private void logError(string errorSource, string errorMessage, string errorContext = "Collect")
        {
            Console.WriteLine("Error: " + instanceName + "{" + dbName + "} - " + errorContext + " - " + errorSource + " : " + errorMessage);
            var rError = dtErrors.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = errorMessage;
            rError["ErrorContext"] = errorContext;
            dtErrors.Rows.Add(rError);
        }


        private void startup(string connectionString, string connectionID)
        {
            _connectionString = connectionString;
            Data = new DataSet("DBADash");
            dtErrors = new DataTable("Errors");
            dtErrors.Columns.Add("ErrorSource");
            dtErrors.Columns.Add("ErrorMessage");
            dtErrors.Columns.Add("ErrorContext");

            Data.Tables.Add(dtErrors);
            GetInstance(connectionID);
        }

        public DBCollector(string connectionString, string connectionID)
        {
            startup(connectionString, connectionID);

        }

        public void RemoveEventSessions()
        {
            if (IsXESupported())
            {
                string removeSQL;
                if (IsAzure)
                {
                    removeSQL = Properties.Resources.SQLRemoveEventSessionsAzure;
                }
                else
                {
                    removeSQL = Properties.Resources.SQLRemoveEventSessions;
                }  
                using (var cn = new SqlConnection(_connectionString))
                {
                    using (var cmd = new SqlCommand(removeSQL, cn))
                    {
                        cn.Open();
                        cmd.ExecuteScalar();
                    }
                }
            }
        }

        public void StopEventSessions()
        {
            if (IsXESupported())
            {
                string removeSQL;
                if (IsAzure)
                {
                    removeSQL = Properties.Resources.SQLStopEventSessionsAzure;
                }
                else
                {
                    removeSQL = Properties.Resources.SQLStopEventSessions;
                }
                using (var cn = new SqlConnection(_connectionString))
                {
                    using (var cmd = new SqlCommand(removeSQL, cn))
                    {
                        cn.Open();
                        cmd.ExecuteScalar();
                    }
                }
            }
        }

        public void GetInstance(string connectionID)
        {
            var dt = getDT("DBADash", Properties.Resources.SQLInstance);
            dt.Columns.Add("AgentVersion", typeof(string));
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Columns.Add("AgentHostName", typeof(string));
            dt.Rows[0]["AgentVersion"] = Assembly.GetEntryAssembly().GetName().Version;
            dt.Rows[0]["AgentHostName"] = Environment.MachineName;

            editionId = (Int64)dt.Rows[0]["EditionId"];
            computerName = (string)dt.Rows[0]["ComputerNamePhysicalNetBIOS"];
            dbName = (string)dt.Rows[0]["DBName"];
            instanceName = (string)dt.Rows[0]["Instance"];
            productVersion = (string)dt.Rows[0]["ProductVersion"];
            string hostPlatform = (string)dt.Rows[0]["host_platform"];
            if (!Enum.TryParse(hostPlatform, out platform))
            {
                logError("Instance", "host_platform parse error");
                platform = HostPlatform.Windows;
            }
            if(platform == HostPlatform.Linux)
            {
                noWMI = true;
            }
            if (editionId == 1674378470)
            {
                IsAzure = true;
                if (dbName == "master")
                {
                    isAzureMasterDB = true;
                }
            }

            if (computerName.Length == 0)
            {
                noWMI = true;
            }
            if (connectionID == null)
            {
                if (IsAzure)
                {
                    dt.Rows[0]["ConnectionID"] = instanceName + "|" + dbName;
                    noWMI = true;
                    // dt.Rows[0]["Instance"] = instanceName + "|" + dbName;
                }
                else
                {
                    dt.Rows[0]["ConnectionID"] = instanceName;
                }
            }
            else
            {
                dt.Rows[0]["ConnectionID"] = connectionID;
            }
            Data.Tables.Add(dt);


        }

        public void Collect(CollectionType[] collectionTypes)
        {
            foreach (CollectionType type in collectionTypes)
            {
                Collect(type);
            }
        }
        private string enumToString(Enum en)
        {
            return Enum.GetName(en.GetType(), en);
        }

        public void Collect(CollectionType collectionType)
        {
            var collectionTypeString = enumToString(collectionType);
            if (Data.Tables.Contains(collectionTypeString))
            {
                // Already collected
                return;
            }
            else if (IsAzure && (!azureCollectionTypes.Contains(collectionType)))
            {
                return;
            }
            else if (collectionType == CollectionType.General)
            {
                Collect(CollectionType.ServerProperties);
                Collect(CollectionType.Databases);
                Collect(CollectionType.DatabasesHADR);
                Collect(CollectionType.SysConfig);
                Collect(CollectionType.Drives);
                Collect(CollectionType.DBFiles);
                Collect(CollectionType.Backups);
                Collect(CollectionType.AgentJobs);
                Collect(CollectionType.LogRestores);
                Collect(CollectionType.ServerExtraProperties);
                Collect(CollectionType.DBConfig);
                Collect(CollectionType.Corruption);
                Collect(CollectionType.OSInfo);
                Collect(CollectionType.TraceFlags);                              
                Collect(CollectionType.DBTuningOptions);
                Collect(CollectionType.AzureDBServiceObjectives);
                Collect(CollectionType.LastGoodCheckDB);
                Collect(CollectionType.Alerts);
                Collect(CollectionType.CustomChecks);
                Collect(CollectionType.DatabaseMirroring);
            }
            else if (collectionType == CollectionType.Performance)
            {
                Collect(CollectionType.ObjectExecutionStats);
                Collect(CollectionType.CPU);
                Collect(CollectionType.BlockingSnapshot);
                Collect(CollectionType.IOStats);
                Collect(CollectionType.Waits);
                Collect(CollectionType.AzureDBResourceStats);
                Collect(CollectionType.AzureDBElasticPoolResourceStats);
                Collect(CollectionType.SlowQueries);
                Collect(CollectionType.PerformanceCounters);
            }
            else if(collectionType == CollectionType.Infrequent)
            {
                Collect(CollectionType.ServerPrincipals);
                Collect(CollectionType.ServerRoleMembers);
                Collect(CollectionType.ServerPermissions);
                Collect(CollectionType.DatabasePrincipals);
                Collect(CollectionType.DatabaseRoleMembers);
                Collect(CollectionType.DatabasePermissions);
                Collect(CollectionType.VLF);
                Collect(CollectionType.DriversWMI);
                Collect(CollectionType.OSLoadedModules);
            }
            else if (collectionType == CollectionType.Drives)
            {
                if (platform == HostPlatform.Windows) // drive collection not supported on linux
                {
                    collectDrives();
                }
            }
            else if (collectionType == CollectionType.ServerExtraProperties)
            {
                collectServerExtraProperties();
            }
            else if (collectionType == CollectionType.DriversWMI)
            {
                collectDriversWMI();
            }
            else if (collectionType == CollectionType.CPU)
            {
                SqlParameter pTop = new SqlParameter("TOP", PerformanceCollectionPeriodMins);
                try
                {
                    addDT(enumToString(collectionType), Properties.Resources.SQLCPU, new SqlParameter[] { pTop });
                }
                catch (Exception ex)
                {
                    logError(collectionTypeString, ex.Message);
                }
            }
            else if (collectionType == CollectionType.AzureDBResourceStats || collectionType == CollectionType.AzureDBServiceObjectives)
            {
                if (IsAzure)
                {
                    SqlParameter pDate = new SqlParameter("Date", DateTime.UtcNow.AddMinutes(-PerformanceCollectionPeriodMins));
                    try
                    {
                        addDT(collectionTypeString, Properties.Resources.ResourceManager.GetString("SQL" + collectionTypeString, Properties.Resources.Culture), new SqlParameter[] { pDate });
                    }
                    catch (Exception ex)
                    {
                        logError(collectionTypeString, ex.Message);
                    }
                }
            }
            else if (collectionType == CollectionType.AzureDBElasticPoolResourceStats)
            {
                if (IsAzure && isAzureMasterDB)
                {
                    SqlParameter pDate = new SqlParameter("Date", DateTime.UtcNow.AddMinutes(-PerformanceCollectionPeriodMins));
                    try
                    {
                        addDT(collectionTypeString, Properties.Resources.ResourceManager.GetString("SQL" + collectionTypeString, Properties.Resources.Culture), new SqlParameter[] { pDate });
                    }
                    catch (Exception ex)
                    {
                        logError(collectionTypeString, ex.Message);
                    }
                }
            }
            else if (collectionType == CollectionType.SlowQueries)
            {
                if (SlowQueryThresholdMs >= 0 && (!(IsAzure && isAzureMasterDB)))
                {
                    var completed = false;
                    var retry = 0;
                    while (!completed)
                    {
                        try
                        {
                            collectSlowQueries();
                            completed = true;
                        }
                        catch (Exception ex)
                        {
                            retry += 1;
                            if (retry > RetryCount)
                            {
                                logError(collectionTypeString, ex.Message);
                                completed = true;
                            }
                            else
                            {
                                logError(collectionTypeString, ex.Message + Environment.NewLine + "Retry in " + RetryInterval.ToString() + "seconds", "Collect[Retrying]");
                                System.Threading.Thread.Sleep(RetryInterval * 1000);
                            }
                        }
                    }
                }
            }
            else if(collectionType == CollectionType.PerformanceCounters)
            {
                var completed = false;
                var retry = 0;
                while (!completed)
                {
                    try
                    {
                        collectPerformanceCounters();
                        completed = true;
                    }
                    catch (Exception ex)
                    {
                        retry += 1;
                        if (retry > RetryCount)
                        {
                            logError(collectionTypeString, ex.Message);
                            completed = true;
                        }
                        else
                        {
                            logError(collectionTypeString, ex.Message + Environment.NewLine + "Retry in " + RetryInterval.ToString() + "seconds", "Collect[Retrying]");
                            System.Threading.Thread.Sleep(RetryInterval * 1000);
                        }
                    }
                }
            }
            else
            {
                var completed = false;
                var retry = 0;
                while (!completed)
                {
                    try
                    {
                        addDT(collectionTypeString, Properties.Resources.ResourceManager.GetString("SQL" + collectionTypeString, Properties.Resources.Culture));
                        completed = true;
                    }
                    catch (Exception ex)
                    {
                        retry += 1;
                        if (retry > RetryCount)
                        {
                            logError(collectionTypeString, ex.Message);
                            completed = true;
                        }
                        else
                        {
                            logError(collectionTypeString, ex.Message + Environment.NewLine + "Retry in " + RetryInterval.ToString() + "seconds","Collect[Retrying]");
                            System.Threading.Thread.Sleep(RetryInterval * 1000);
                        }
                    }
                }

            }
        }

        private void collectPerformanceCounters()
        {
  
            string xml = PerformanceCounters.PerformanceCountersXML;
            if (xml.Length > 0)
            {
       
                string sql = Properties.Resources.ResourceManager.GetString("SQLPerformanceCounters", Properties.Resources.Culture);
                using (var cn = new SqlConnection(_connectionString))
                {
                    using (var da = new SqlDataAdapter(sql, cn))
                    {
                        cn.Open();
                        var ds = new DataSet();
                        SqlParameter pCountersXML = new SqlParameter("CountersXML", PerformanceCounters.PerformanceCountersXML)
                        {
                            SqlDbType = SqlDbType.Xml
                        };
                        da.SelectCommand.CommandTimeout = 60;
                        da.SelectCommand.Parameters.Add(pCountersXML);
                        da.Fill(ds);


                        var dt = ds.Tables[0];
                        if (ds.Tables.Count == 2)
                        {
                            var userDT = ds.Tables[1];
                            if (dt.Columns.Count == userDT.Columns.Count)
                            {
                                try
                                {
                                    for (Int32 i = 0; i < (dt.Columns.Count - 1); i++)
                                    {
                                        if (dt.Columns[i].ColumnName != userDT.Columns[i].ColumnName)
                                        {
                                            throw new Exception(String.Format("Invalid schema for custom metrics.  Expected column '{0}' in position {1} instead of '{2}'", dt.Columns[i].ColumnName, i + 1, userDT.Columns[i].ColumnName));
                                        }
                                        if (dt.Columns[i].DataType != userDT.Columns[i].DataType)
                                        {
                                            throw new Exception(String.Format("Invalid schema for custom metrics.  Column {0} expected data type is {1} instead of {2}", dt.Columns[i].ColumnName, dt.Columns[i].DataType.Name, userDT.Columns[i].DataType.Name));
                                        }
                                    }
                                    dt.Merge(userDT);
                                }
                                catch (Exception ex)
                                {
                                    logError("PerformanceCounters", ex.Message);
                                }
                            }
                            else
                            {
                                logError("PerformanceCounters", String.Format("Invalid schema for custom metrics. Expected {0} columns instead of {1}.", dt.Columns.Count, userDT.Columns.Count));
                            }
                        }
                        ds.Tables.Remove(dt);
                        dt.TableName = "PerformanceCounters";
                        Data.Tables.Add(dt);
                    }
                }
            }
            
       
        }


        private void collectSlowQueries()
        {

            if (IsXESupported())
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connectionString)
                {
                    ApplicationName = "DBADashXE"
                };
                string slowQueriesSQL;
                if (IsAzure)
                {
                    slowQueriesSQL = Properties.Resources.SQLSlowQueriesAzure;
                }
                else
                {
                    slowQueriesSQL = Properties.Resources.SQLSlowQueries;
                }
                using (var cn = new SqlConnection(builder.ConnectionString))
                {
                    using (var cmd = new SqlCommand(slowQueriesSQL, cn) { CommandTimeout = 90 })
                    {
                        cn.Open();

                        cmd.Parameters.AddWithValue("SlowQueryThreshold", SlowQueryThresholdMs * 1000);
                        cmd.Parameters.AddWithValue("MaxMemory", SlowQueryMaxMemoryKB);
                        cmd.Parameters.AddWithValue("UseDualSession", UseDualEventSession);
                        var result = cmd.ExecuteScalar();
                        if (result == DBNull.Value)
                        {
                            logError("SlowQueries", "Result IS NULL");
                            return;
                        }
                        string ringBuffer = (string)result;
                        if (ringBuffer.Length > 0)
                        {
                            var dt = XETools.XEStrToDT(ringBuffer, out RingBufferTargetAttributes ringBufferAtt);
                            dt.TableName = "SlowQueries";
                            addDT(dt);
                            var dtAtt = ringBufferAtt.GetTable();
                            dtAtt.TableName = "SlowQueriesStats";
                            addDT(dtAtt);

                        }
                    }
                }
            }
        }


        private void collectServerExtraProperties()
        {
            try
            {

                if (!this.IsAzure)
                {
                    if (!noWMI)
                    {
                        collectComputerSystemWMI();
                        collectOperatingSystemWMI();
                    }
                    addDT("ServerExtraProperties", DBADash.Properties.Resources.SQLServerExtraProperties);
                    Data.Tables["ServerExtraProperties"].Columns.Add("WindowsCaption");
                    if (manufacturer != "") { Data.Tables["ServerExtraProperties"].Rows[0]["SystemManufacturer"] = manufacturer; }
                    if (model != "") { Data.Tables["ServerExtraProperties"].Rows[0]["SystemProductName"] = model; }
                    Data.Tables["ServerExtraProperties"].Rows[0]["WindowsCaption"] = WindowsCaption;
                    if (Data.Tables["ServerExtraProperties"].Rows[0]["ActivePowerPlanGUID"] == DBNull.Value && noWMI == false)
                    {
                        collectPowerPlanWMI();
                        Data.Tables["ServerExtraProperties"].Rows[0]["ActivePowerPlanGUID"] = activePowerPlanGUID;
                        Data.Tables["ServerExtraProperties"].Rows[0]["ActivePowerPlan"] = activePowerPlan;
                    }
                }
            }
            catch (Exception ex)
            {
                logError("ServerExtraProperties", ex.Message);
            }
        }


        public DataTable getDT(string tableName, string SQL, SqlParameter[] param = null)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                using (var da = new SqlDataAdapter(SQL,cn)) {
                    cn.Open();
                    DataTable dt = new DataTable();
                    da.SelectCommand.CommandTimeout = 60;
                    if (param != null)
                    {
                        da.SelectCommand.Parameters.AddRange(param);
                    }
                    da.Fill(dt);
                    dt.TableName = tableName;
                    return dt;
                }

            }
        }

        public void addDT(string tableName, string sql, SqlParameter[] param = null)
        {
            if (!Data.Tables.Contains(tableName))
            {

                Data.Tables.Add(getDT(tableName, sql, param));

            }
        }

        private void addDT(DataTable dt)
        {
            if (!Data.Tables.Contains(dt.TableName))
            {
                Data.Tables.Add(dt);
            }
        }


        public void collectDrivesSQL()
        {
            try
            {
                addDT("Drives", Properties.Resources.SQLDrives);
            }
            catch (Exception ex)
            {
                logError("Drives", ex.Message);
            }
        }

        public void collectDrives()
        {

            if (noWMI)
            {
                collectDrivesSQL();
            }
            else
            {
                try
                {
                    collectDrivesWMI();
                }
                catch (Exception ex)
                {
                    logError("Drives", "Error collecting drives via WMI.  Drive info will be collected from SQL, but might be incomplete.  Use --nowmi switch to collect through SQL as default." + Environment.NewLine + ex.Message, "Collect:WMI");
                    collectDrivesSQL();
                }
            }
        }

        string activePowerPlan;
        Guid activePowerPlanGUID;
        string manufacturer;
        string model;
        string WindowsCaption;

        #region "WMI"

        private void collectOperatingSystemWMI()
        {
            if (!noWMI)
            {
                try
                {
                    ManagementPath path = new ManagementPath()
                    {
                        NamespacePath = @"root\cimv2",
                        Server = computerName
                    };
                    ManagementScope scopeCIMV2 = new ManagementScope(path);

                    SelectQuery query = new SelectQuery("Win32_OperatingSystem", "", new string[] { "Caption" });
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scopeCIMV2, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        if (results.Count == 1)
                        {
                            var mo = results.OfType<ManagementObject>().FirstOrDefault();
                            if (mo != null)
                            {
                                WindowsCaption = getMOStringValue(mo, "Caption", 256);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logError("ServerExtraProperties", ex.Message, "Collect:Win32_OperatingSystem WMI");
                }
            }
        }

        private void collectComputerSystemWMI()
        {
            if (!noWMI)
            {
                try
                {
                    ManagementPath path = new ManagementPath()
                    {
                        NamespacePath = @"root\cimv2",
                        Server = computerName
                    };
                    ManagementScope scopeCIMV2 = new ManagementScope(path);

                    SelectQuery query = new SelectQuery("Win32_ComputerSystem", "", new string[] { "Manufacturer", "Model" });
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scopeCIMV2, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        if (results.Count == 1)
                        {
                            var mo = results.OfType<ManagementObject>().FirstOrDefault();
                            if (mo != null)
                            {
                                manufacturer = getMOStringValue(mo, "Manufacturer", 200);
                                model = getMOStringValue(mo, "Model", 200);

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logError("ServerExtraProperties", ex.Message, "Collect:Win32_ComputerSystem WMI");
                }
            }
        }

        private string getMOStringValue(ManagementObject mo, string propertyName, Int32 truncateLength = 0)
        {
            string value = "";
            if (mo.GetPropertyValue(propertyName) != null)
            {
                value = mo.GetPropertyValue(propertyName).ToString();
                if (truncateLength > 0 && value.Length > truncateLength)
                {
                    value = value.Substring(0, 200);
                }
            }
            return value;
        }

        private void collectPowerPlanWMI()
        {
            if (!noWMI)
            {
                try
                {
                    ManagementPath pathPower = new ManagementPath()
                    {
                        NamespacePath = @"root\cimv2\power",
                        Server = computerName
                    };
                    ManagementScope scopePower = new ManagementScope(pathPower);
                    SelectQuery query = new SelectQuery("Win32_PowerPlan", "IsActive=1", new string[] { "InstanceID", "ElementName" });
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scopePower, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {

                        var mo = results.OfType<ManagementObject>().FirstOrDefault();
                        if (mo != null)
                        {


                            string instanceId = getMOStringValue(mo, "InstanceID");
                            if (instanceId.Length > 0)
                            {
                                activePowerPlanGUID = Guid.Parse(instanceId.Substring(instanceId.Length - 38, 38));
                            }
                            activePowerPlan = getMOStringValue(mo, "ElementName");
                        }

                    }
                }
                catch (Exception ex)
                {
                    logError("ServerExtraProperties", ex.Message, "Collect:Win32_PowerPlan WMI");
                }
            }
        }

        private void collectDriversWMI()
        {
            if (!noWMI)
            {
                try
                {
                    if (!Data.Tables.Contains("Drivers"))
                    {
                        DataTable dtDrivers = new DataTable("Drivers");
                        string[] selectedProperties = new string[] { "ClassGuid", "DeviceClass", "DeviceID", "DeviceName", "DriverDate", "DriverProviderName", "DriverVersion", "FriendlyName", "HardWareID", "Manufacturer", "PDO" };
                        foreach (string p in selectedProperties)
                        {
                            if (p == "DriverDate")
                            {
                                dtDrivers.Columns.Add(p, typeof(DateTime));
                            }
                            else if (p == "ClassGuid")
                            {
                                dtDrivers.Columns.Add(p, typeof(Guid));
                            }
                            else
                            {
                                dtDrivers.Columns.Add(p, typeof(string));
                            }
                        }

                        ManagementPath path = new ManagementPath()
                        {
                            NamespacePath = @"root\cimv2",
                            Server = computerName
                        };
                        ManagementScope scope = new ManagementScope(path);

                        SelectQuery query = new SelectQuery("Win32_PnPSignedDriver", "", selectedProperties);

                        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                        using (ManagementObjectCollection results = searcher.Get())
                        {
                            foreach (ManagementObject mo in results)
                            {
                                if (mo != null)
                                {
                                    var rDriver = dtDrivers.NewRow();
                                    foreach (string p in selectedProperties)
                                    {
                                        if (mo.GetPropertyValue(p) != null)
                                        {
                                            if (p == "DriverDate" || p == "InstallDate")
                                            {

                                                try
                                                {
                                                    rDriver[p] = ManagementDateTimeConverter.ToDateTime(mo.GetPropertyValue(p).ToString());
                                                }
                                                catch (Exception ex)
                                                {
                                                    logError("Drivers", p + ": " + ex.Message);
                                                }
                                            }

                                            else if (p == "ClassGuid")
                                            {
                                                try
                                                {
                                                    rDriver[p] = Guid.Parse(mo.GetPropertyValue(p).ToString());
                                                }
                                                catch (Exception ex)
                                                {
                                                    logError("Drivers", p + ": " + ex.Message);
                                                }

                                            }
                                            else
                                            {
                                                try
                                                {
                                                    string value = mo.GetPropertyValue(p).ToString();

                                                    rDriver[p] = value.Length <= 200 ? value : value.Substring(0, 200);

                                                }
                                                catch (Exception ex)
                                                {
                                                    logError("Drivers", p + ": " + ex.Message);
                                                }

                                            }

                                        }
                                    }
                                    dtDrivers.Rows.Add(rDriver);
                                }
                            }
                        }
                        try
                        {
                            var PVKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computerName, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Amazon\\PVDriver");
                            if (PVKey != null)
                            {
                                var rDriver = dtDrivers.NewRow();
                                rDriver["DeviceID"] = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Amazon\\PVDriver";
                                rDriver["Manufacturer"] = "Amazon Inc.";
                                rDriver["DriverProviderName"] = "Amazon Inc.";
                                rDriver["DeviceName"] = "AWS PV Driver";
                                rDriver["DriverVersion"] = PVKey.GetValue("Version");
                                dtDrivers.Rows.Add(rDriver);
                            }
                        }
                        catch (Exception ex)
                        {
                            logError("Drivers", ex.Message,"Collect:AWSPVDriver");
                        }
                        Data.Tables.Add(dtDrivers);
                    }

                }
                catch (Exception ex)
                {
                    logError("Drivers", ex.Message, "Collect:WMI");
                }
            }
        }

        private void collectDrivesWMI()
        {
            try
            {
                if (!Data.Tables.Contains("Drives"))
                {
                    DataTable drives = new DataTable("Drives");
                    drives.Columns.Add("Name", typeof(string));
                    drives.Columns.Add("Capacity", typeof(Int64));
                    drives.Columns.Add("FreeSpace", typeof(Int64));
                    drives.Columns.Add("Label", typeof(string));

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
            catch (Exception ex)
            {
                logError("Drives", ex.Message, "Collect:WMI");
            }
        }

        #endregion

    }
}
