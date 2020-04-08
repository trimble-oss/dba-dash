using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Management;
using System.Reflection;

namespace DBAChecks
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CollectionType
    {
        General,
        Performance,
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
        ProcStats,
        FunctionStats,
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
        AzureDBResourceStats
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
        CollectionType[] azureCollectionTypes = new CollectionType[] {CollectionType.AzureDBResourceStats,CollectionType.CPU, CollectionType.DBFiles, CollectionType.General, CollectionType.Performance, CollectionType.Databases, CollectionType.DBConfig, CollectionType.TraceFlags, CollectionType.ProcStats, CollectionType.FunctionStats, CollectionType.BlockingSnapshot, CollectionType.IOStats, CollectionType.Waits, CollectionType.ServerProperties, CollectionType.DBTuningOptions ,CollectionType.SysConfig};


        public bool IsAzure
        {
            get
            {
                if (editionId == 1674378470)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }



        public DBCollector(string connectionString, bool noWMI)
        {
            this.noWMI = noWMI;
            startup(connectionString, null);       
        }

        private void logError(string errorSource, string errorMessage)
        {
            Console.WriteLine(errorSource + " : " + errorMessage);
            var rError = dtErrors.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = errorMessage;
            dtErrors.Rows.Add(rError);
        }


        private void startup(string connectionString, string connectionID)
        {
            _connectionString = connectionString;
            Data = new DataSet("DBAChecks");
            dtErrors = new DataTable("Errors");
            dtErrors.Columns.Add("ErrorSource");
            dtErrors.Columns.Add("ErrorMessage");

            Data.Tables.Add(dtErrors);
            GetInstance(connectionID);
        }

        public DBCollector(string connectionString, string connectionID)
        {
            startup(connectionString, connectionID);

        }

        public void GetInstance(string connectionID)
        {
            var dt = getDT("DBAChecks", "SELECT @@SERVERNAME as Instance,GETUTCDATE() As SnapshotDateUTC,CAST(SERVERPROPERTY('EditionID') as bigint) as EditionID,ISNULL(CAST(SERVERPROPERTY('ComputerNamePhysicalNetBIOS') as nvarchar(128)),'') as ComputerNamePhysicalNetBIOS,DB_NAME() as DBName");
            dt.Columns.Add("AgentVersion", typeof(string));
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Columns.Add("AgentHostName", typeof(string));
            dt.Rows[0]["AgentVersion"] = Assembly.GetEntryAssembly().GetName().Version;
            dt.Rows[0]["AgentHostName"] = Environment.MachineName;
       
            editionId = (Int64)dt.Rows[0]["EditionId"];
            computerName = (string)dt.Rows[0]["ComputerNamePhysicalNetBIOS"];
 
            string dbName = (string)dt.Rows[0]["DBName"];
            string instanceName = (string)dt.Rows[0]["Instance"];
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
                Collect(CollectionType.DriversWMI);
                Collect(CollectionType.OSLoadedModules);
                Collect(CollectionType.DBTuningOptions);
            }
            else if (collectionType == CollectionType.Performance)
            {
                Collect(CollectionType.ProcStats);
                Collect(CollectionType.FunctionStats);
                Collect(CollectionType.CPU);
                Collect(CollectionType.BlockingSnapshot);
                Collect(CollectionType.IOStats);
                Collect(CollectionType.Waits);
                Collect(CollectionType.AzureDBResourceStats);
            }
            else if (collectionType == CollectionType.Drives)
            {
                collectDrives();
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
                catch(Exception ex)
                {
                    logError(collectionTypeString, ex.Message);
                }
            }
            else if(collectionType==CollectionType.AzureDBResourceStats)
            {
                if (IsAzure)
                {
                    SqlParameter pDate = new SqlParameter("Date", DateTime.UtcNow.AddMinutes(-PerformanceCollectionPeriodMins));
                    try
                    {
                        addDT(collectionTypeString, Properties.Resources.SQLAzureDBResourceStats, new SqlParameter[] { pDate });
                    }
                    catch(Exception ex)
                    {
                        logError(collectionTypeString, ex.Message);
                    }
                }
            }
            else
            {
                try
                {
                    addDT(collectionTypeString, Properties.Resources.ResourceManager.GetString("SQL" + collectionTypeString, Properties.Resources.Culture));               
                }
                catch (Exception ex)
                {
                    logError(collectionTypeString, ex.Message);
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
                    addDT("ServerExtraProperties", DBAChecks.Properties.Resources.SQLServerExtraProperties);
                    Data.Tables["ServerExtraProperties"].Columns.Add("WindowsCaption");
                    if (manufacturer != "") { Data.Tables["ServerExtraProperties"].Rows[0]["SystemManufacturer"] = manufacturer; }
                    if (model != "") { Data.Tables["ServerExtraProperties"].Rows[0]["SystemProductName"] = model; }
                    if (WindowsVersion != "") { Data.Tables["ServerExtraProperties"].Rows[0]["WindowsRelease"] = WindowsVersion; }
                    if (WindowsSP != "") { Data.Tables["ServerExtraProperties"].Rows[0]["WindowsServicePackLevel"] = WindowsSP; }
                    if (Data.Tables["ServerExtraProperties"].Rows[0]["WindowsSKU"] == DBNull.Value) { Data.Tables["ServerExtraProperties"].Rows[0]["WindowsSKU"] = WindowsSKU; }
                    Data.Tables["ServerExtraProperties"].Rows[0]["WindowsCaption"] = WindowsCaption;
                    if (Data.Tables["ServerExtraProperties"].Rows[0]["ActivePowerPlanGUID"] == DBNull.Value && noWMI==false)
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

        public void addDT(string tableName, string sql, SqlParameter[] param = null)
        {
            if (!Data.Tables.Contains(tableName))
            {

                Data.Tables.Add(getDT(tableName, sql, param));

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

        public void collectDrivesSQL()
        {
            try
            {
                addDT("Drives", Properties.Resources.SQLDrives);
            }
            catch (Exception ex)
            {
                logError("Collect Drives(SQL)", ex.Message);
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
                    logError("Collect Drives", "Error collecting drives via WMI.  Drive info will be collected from SQL, but might be incomplete.  Use --nowmi switch to collect through SQL as default.");
                    collectDrivesSQL();
                }
            }
        }

        string activePowerPlan;
        Guid activePowerPlanGUID;
        string manufacturer;
        string model;
        string WindowsVersion;
        string WindowsCaption;
        Int32 WindowsSKU;
        string WindowsSP;


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

                    SelectQuery query = new SelectQuery("Win32_OperatingSystem", "", new string[] { "Version", "Caption", "OperatingSystemSKU", "ServicePackMajorVersion", "ServicePackMinorVersion" });
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scopeCIMV2, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        if (results.Count == 1)
                        {
                            var mo = results.OfType<ManagementObject>().FirstOrDefault();
                            if (mo != null)
                            {
                                WindowsVersion = getMOStringValue(mo, "Version", 256);
                                WindowsCaption = getMOStringValue(mo, "Caption", 256);
                                WindowsSKU = Int32.Parse(getMOStringValue(mo, "OperatingSystemSKU"));
                                WindowsSP = getMOStringValue(mo, "ServicePackMajorVersion", 128) + "." + getMOStringValue(mo, "ServicePackMinorVersion", 127);

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logError("Win32_OperatingSystem WMI", ex.Message);
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
                    logError("Win32_ComputerSystem WMI", ex.Message);
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
                    logError("Win32_PowerPlan WMI", ex.Message);
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
                            logError("AWSPVDriver", ex.Message);
                        }
                        Data.Tables.Add(dtDrivers);
                    }

                }
                catch (Exception ex)
                {
                    logError("Drivers (WMI)", ex.Message);
                    throw ex;
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
                logError("Collect Drives (WMI)", ex.Message);
                throw ex;
            }
        }

        #endregion

    }
}
