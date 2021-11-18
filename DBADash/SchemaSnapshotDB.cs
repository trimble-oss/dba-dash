using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Broker;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Specialized;
using System.Data;
using  Microsoft.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Serilog;
using SerilogTimings;

namespace DBADash
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SchemaSnapshotDBObjectTypes
    {
        Database,
        StoredProcedures,
        Tables,
        Aggregate,
        Assembly,
        UserDefinedType,
        XMLSchema,
        Schema,
        UserDefinedFunction,
        View,
        UserDefinedTableType,
        UserDefinedDataType,
        DDLTrigger,
        Synonym,
        Roles,
        Users,
        ApplicationRole,
        Sequence,
        ServiceBroker,
        Trigger
    }

  
    public class SchemaSnapshotDBOptions
    {
    
        public bool DriAll =true;
        public bool Triggers = true;
        public bool FullTextIndexes = true;
        public bool Indexes = true;
        public bool XMLIndexes = true;
        public bool ExtendedProperties = true;
        public bool Statistics = true;
        public bool DriIncludeSystemNames = false;
        public bool Permissions = false;
       
        public SchemaSnapshotDBObjectTypes[] ObjectTypes = DefaultSchemaSnapshotDBObjectTypes();

        public static SchemaSnapshotDBObjectTypes[] DefaultSchemaSnapshotDBObjectTypes()
        {
            return new SchemaSnapshotDBObjectTypes[] {SchemaSnapshotDBObjectTypes.Database, SchemaSnapshotDBObjectTypes.Aggregate, SchemaSnapshotDBObjectTypes.Assembly, SchemaSnapshotDBObjectTypes.DDLTrigger, SchemaSnapshotDBObjectTypes.Schema, SchemaSnapshotDBObjectTypes.StoredProcedures, SchemaSnapshotDBObjectTypes.Synonym, SchemaSnapshotDBObjectTypes.Tables, SchemaSnapshotDBObjectTypes.UserDefinedDataType, SchemaSnapshotDBObjectTypes.UserDefinedFunction, SchemaSnapshotDBObjectTypes.UserDefinedTableType, SchemaSnapshotDBObjectTypes.UserDefinedType, SchemaSnapshotDBObjectTypes.View, SchemaSnapshotDBObjectTypes.XMLSchema , SchemaSnapshotDBObjectTypes.Roles, SchemaSnapshotDBObjectTypes.ApplicationRole, SchemaSnapshotDBObjectTypes.Sequence, SchemaSnapshotDBObjectTypes.ServiceBroker,  SchemaSnapshotDBObjectTypes.Trigger};
        }


        public ScriptingOptions ScriptOptions()
        {
            var so = new ScriptingOptions
            {
                DriAll = DriAll,
                Triggers = Triggers,
                FullTextIndexes = FullTextIndexes,
                Indexes = Indexes,
                XmlIndexes = XMLIndexes,
                ExtendedProperties = ExtendedProperties,
                Statistics = Statistics,
                DriIncludeSystemNames = DriIncludeSystemNames,
                Permissions = Permissions
            };
            return so;
        }

    }

    public class SchemaSnapshotDB
    {

        private readonly string _connectionString;
        private readonly SchemaSnapshotDBOptions options;
        private readonly ScriptingOptions ScriptingOptions;
        private string masterConnectionString
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(_connectionString)
                {
                    InitialCatalog = "master"
                };
                return builder.ConnectionString;
            }
        }

        private byte[] computeHash(byte[] obj)
        {
            using(var crypt = SHA256.Create())
            {
                return crypt.ComputeHash(obj); 
            }
        }
       
        public SchemaSnapshotDB(string connectionString,SchemaSnapshotDBOptions options)
        {
            _connectionString = connectionString;
            options = options == null ? new SchemaSnapshotDBOptions() : options;
            this.options = options;
            this.ScriptingOptions = options.ScriptOptions();
        }

        public SchemaSnapshotDB(string connectionString)
        {
            _connectionString = connectionString;
            this.options = new SchemaSnapshotDBOptions();
            this.ScriptingOptions = options.ScriptOptions();
        }


        private static string stringCollectionToString(StringCollection sc)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var s in sc)
            {
                sb.AppendLine(s);
                sb.AppendLine("GO");
            }
            return sb.ToString();
        }


        private DataTable JobDataTableSchema()
        {
            DataTable dtSchema = new DataTable("Jobs");
            dtSchema.Columns.Add("job_id", typeof(Guid));
            dtSchema.Columns.Add("originating_server");
            dtSchema.Columns.Add("name");
            dtSchema.Columns.Add("enabled", typeof(bool));
            dtSchema.Columns.Add("description");
            dtSchema.Columns.Add("start_step_id", typeof(int));
            dtSchema.Columns.Add("category_id", typeof(int));
            dtSchema.Columns.Add("category");
            dtSchema.Columns.Add("owner");
            dtSchema.Columns.Add("notify_level_eventlog", typeof(int));
            dtSchema.Columns.Add("notify_level_email", typeof(int));
            dtSchema.Columns.Add("notify_level_netsend", typeof(int));
            dtSchema.Columns.Add("notify_level_page", typeof(int));
            dtSchema.Columns.Add("notify_email_operator");
            dtSchema.Columns.Add("notify_netsend_operator");
            dtSchema.Columns.Add("notify_page_operator");
            dtSchema.Columns.Add("delete_level", typeof(int));
            dtSchema.Columns.Add("date_created", typeof(DateTime));
            dtSchema.Columns.Add("date_modified", typeof(DateTime));
            dtSchema.Columns.Add("version_number", typeof(int));
            dtSchema.Columns.Add("has_schedule", typeof(bool));
            dtSchema.Columns.Add("has_server", typeof(bool));
            dtSchema.Columns.Add("has_step", typeof(bool));
            dtSchema.Columns.Add("DDLHash", typeof(byte[]));
            dtSchema.Columns.Add("DDL", typeof(byte[]));
            return dtSchema;
        }

        private DataTable JobStepTableSchema()
        {
            var jobStepDT = new DataTable("JobSteps");
            jobStepDT.Columns.Add("job_id", typeof(Guid));
            jobStepDT.Columns.Add("step_id", typeof(int));
            jobStepDT.Columns.Add("step_name");
            jobStepDT.Columns.Add("subsystem");
            jobStepDT.Columns.Add("command");
            jobStepDT.Columns.Add("cmdexec_success_code", typeof(int));
            jobStepDT.Columns.Add("on_success_action", typeof(short));
            jobStepDT.Columns.Add("on_success_step_id", typeof(int));
            jobStepDT.Columns.Add("on_fail_action", typeof(short));
            jobStepDT.Columns.Add("on_fail_step_id", typeof(int));
            jobStepDT.Columns.Add("database_name");
            jobStepDT.Columns.Add("database_user_name");
            jobStepDT.Columns.Add("retry_attempts", typeof(int));
            jobStepDT.Columns.Add("retry_interval", typeof(int));
            jobStepDT.Columns.Add("output_file_name");
            jobStepDT.Columns.Add("proxy_name");
            return jobStepDT;
        }

        public void SnapshotJobs(ref DataSet ds)
        {
            var jobDT = JobDataTableSchema();
            var jobStepDT = JobStepTableSchema();

            using (var cn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                var instance = new Microsoft.SqlServer.Management.Smo.Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
                foreach(Microsoft.SqlServer.Management.Smo.Agent.Job job in instance.JobServer.Jobs)
                {
                    DataRow r = jobDT.NewRow();
                    var sDDL = stringCollectionToString(job.Script(ScriptingOptions));
                   
                    var bDDL = Zip(sDDL);
                    r["name"] = job.Name;
                    r["job_id"] = job.JobID;
                    r["enabled"] = job.IsEnabled;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["date_created"] = job.DateCreated;
                    r["date_modified"] = job.DateLastModified;
                    r["category"] = job.Category;
                    r["category_id"] = job.CategoryID;
                    r["description"] = job.Description;
                    r["version_number"] = job.VersionNumber;
                    r["delete_level"] = job.DeleteLevel;
                    r["notify_level_page"] = job.DeleteLevel;
                    r["notify_level_netsend"] = job.NetSendLevel;
                    r["notify_level_email"] = job.EmailLevel;
                    r["notify_level_eventlog"] = job.EventLogLevel;
                    r["notify_email_operator"] = job.OperatorToEmail;
                    r["notify_netsend_operator"] = job.OperatorToNetSend;
                    r["notify_page_operator"] = job.OperatorToPage;
                    r["owner"] = job.OwnerLoginName;
                    r["start_step_id"] = job.StartStepID;
                    r["has_schedule"] = job.HasSchedule;
                    r["has_server"] = job.HasServer;
                    r["has_step"] = job.HasSchedule;
                    jobDT.Rows.Add(r);

                    foreach(Microsoft.SqlServer.Management.Smo.Agent.JobStep step in job.JobSteps)
                    {
                        var stepR = jobStepDT.NewRow();
                        stepR["job_id"] = job.JobID;
                        stepR["step_name"] = step.Name;
                        stepR["database_name"] = step.DatabaseName;
                        stepR["step_id"] = step.ID;
                        stepR["subsystem"] = step.SubSystem;
                        stepR["command"] = step.Command;
                        stepR["cmdexec_success_code"] = step.CommandExecutionSuccessCode;
                        stepR["on_success_action"] = step.OnSuccessAction;
                        stepR["on_success_step_id"] = step.OnSuccessStep;
                        stepR["on_fail_action"] = step.OnFailAction;
                        stepR["on_fail_step_id"] = step.OnFailStep;
                        stepR["database_user_name"] = step.DatabaseUserName;
                        stepR["retry_attempts"] = step.RetryAttempts;
                        stepR["retry_interval"] = step.RetryInterval;
                        stepR["output_file_name"] = step.OutputFileName;
                        stepR["proxy_name"] = step.ProxyName;

                        jobStepDT.Rows.Add(stepR);

                    }
                }
            }
            ds.Tables.Add(jobDT);
            ds.Tables.Add(jobStepDT);
        }

        private DataTable rgDTSchema()
        {
            DataTable dtRG = new DataTable
            {
                TableName = "ResourceGovernorConfiguration"
            };
            dtRG.Columns.Add("is_enabled", typeof(bool));
            dtRG.Columns.Add("classifier_function", typeof(string));          
            dtRG.Columns.Add("reconfiguration_error", typeof(bool));
            dtRG.Columns.Add("reconfiguration_pending", typeof(bool));
            dtRG.Columns.Add("max_outstanding_io_per_volume",typeof(int));
            dtRG.Columns.Add("script", typeof(string));
            return dtRG;
        }

        public DataTable ResourceGovernorConfiguration()
        {
            DataTable dtRG = rgDTSchema();
            bool reconfigError = false;
            using (var cn = new Microsoft.Data.SqlClient.SqlConnection(masterConnectionString))
            {
                var instance = new Microsoft.SqlServer.Management.Smo.Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
                if (instance.EngineEdition == Edition.EnterpriseOrDeveloper) //  RG is an enterprise only edition feature
                {
                    var rg = instance.ResourceGovernor;
                    // Script RG configuration
                    var sc = rg.Script();
                    // Add classifier function script
                    sc.Insert(0,ObjectDDL(masterConnectionString, rg.ClassifierFunction));

                    // Script out resource pool configuration and workload groups
                    foreach (ResourcePool pool in rg.ResourcePools)
                    {
                        if (!(pool.IsSystemObject && pool.ID == 1)) // Ignore internal pool which can't be configured
                        {
                            try
                            {
                                var poolSc = pool.Script();
                                sc.AppendCollection(poolSc);
                                foreach (WorkloadGroup wg in pool.WorkloadGroups)
                                {
                                    sc.AppendCollection(wg.Script());
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex.InnerException.Message == "The resource governor resource pool information is not complete. This can happen if a pool was created but the resource governor is not reconfigured.")
                                {
                                    // Might have an issue scripting if a pool is created without running ALTER RESOURCE GOVERNOR RECONFIGURE.  Ignore this error and add a comment to the script
                                    sc.Add($"/* Unable to script pool {pool.Name} until resource governor is reconfigured */");
                                    reconfigError = true;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                    if (rg.ServerVersion.Major >= 13) // Supported 2016 and later
                    {
                        foreach (ExternalResourcePool pool in rg.ExternalResourcePools)
                        {
                            var poolSc = pool.Script();
                            sc.AppendCollection(poolSc);
                        }
                    }
                    var row = dtRG.NewRow();
                    row["is_enabled"] = rg.Enabled;
                    row["classifier_function"] = rg.ClassifierFunction;
                    row["reconfiguration_error"] = reconfigError;
                    row["reconfiguration_pending"] = rg.ReconfigurePending;
                    row["max_outstanding_io_per_volume"] =rg.ServerVersion.Major >=12 ? rg.MaxOutstandingIOPerVolume : 0; // Supported 2014 and later
                    row["script"] = stringCollectionToString(sc);
                    dtRG.Rows.Add(row);
                    
                }

            }
            return dtRG;
        }

        public static string ObjectDDL(string connectionString, string objectName)
        {
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SELECT OBJECT_DEFINITION(OBJECT_ID(@ObjectName)) as DDL", cn))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@ObjectName", objectName);
                return Convert.ToString(cmd.ExecuteScalar());
            }
        }

        public DataTable SnapshotDB(string DBName)
        {

            using (var cn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            using (var opSS = Operation.Begin("Schema snapshot {DBame}", DBName))
            {
                DataTable dtSchema = new DataTable("Schema_" + DBName);
                dtSchema.Columns.Add("ObjectName");
                dtSchema.Columns.Add("SchemaName");
                dtSchema.Columns.Add("ObjectType");
                dtSchema.Columns.Add("object_id", typeof(Int32));
                dtSchema.Columns.Add("DDLHash", typeof(byte[]));
                dtSchema.Columns.Add("DDL", typeof(byte[]));
                dtSchema.Columns.Add("ObjectDateCreated", typeof(DateTime));
                dtSchema.Columns.Add("ObjectDateModified", typeof(DateTime));
                var instance = new Microsoft.SqlServer.Management.Smo.Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
                instance.SetDefaultInitFields(typeof(Table), true);

                var db = instance.Databases[DBName];
                string sDDL;
                byte[] bDDL;
                DataRow r;
                if (db.IsUpdateable && db.IsAccessible)
                {
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Database))
                    {
                        sDDL = stringCollectionToString(db.Script(ScriptingOptions));
                        bDDL = Zip(sDDL);
                        r = dtSchema.NewRow();
                        r["ObjectName"] = "Database";
                        r["SchemaName"] = "";
                        r["ObjectType"] = "DB";
                        r["object_id"] = 0;
                        r["DDL"] = bDDL;
                        r["DDLHash"] = computeHash(bDDL);
                        r["ObjectDateCreated"] = db.CreateDate;
                        r["ObjectDateModified"] = DBNull.Value;
                        dtSchema.Rows.Add(r);
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Aggregate))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Aggregate"))
                        {
                            addAggregate(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Assembly))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Assembly"))
                        {
                            addAssembly(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedType))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "UserDefinedType"))
                        {
                            addUserDefinedType(db, dtSchema); ;
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.XMLSchema))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "XMLSchema"))
                        {
                            addXMLSchema(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Schema))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Schema"))
                        {
                            addSchema(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Tables))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Tables"))
                        {
                            addTables(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.StoredProcedures))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "StoredProcedures"))
                        {
                            addSPs(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedFunction))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "UserDefinedFunctions"))
                        {
                            addUDFs(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.View))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Views"))
                        {
                            addViews(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedTableType))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "UserDefinedTableTypes"))
                        {
                            addUserDefinedTableType(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedDataType))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "UserDefinedDataTypes"))
                        {
                            addUserDefinedDataType(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.DDLTrigger))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "DDLTriggers"))
                        {
                            addDDLTriggers(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Synonym))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Synonyms"))
                        {
                            addSynonyms(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Roles))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Roles"))
                        {
                            addRoles(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Users))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Users"))
                        {
                            addUsers(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.ApplicationRole))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "ApplicationRoles"))
                        {
                            addAppRole(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Sequence))
                    {
                        using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "Sequences"))
                        {
                            addSeq(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.ServiceBroker))
                    {
                        if (instance.ServerType != Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase)
                        {
                            using (var op = Operation.Begin("Schema snapshot {DBame}: {Object}", DBName, "ServiceBroker"))
                            {
                                addServiceBroker(db, dtSchema);
                                op.Complete();
                            }
                        }

                    }
                }
                opSS.Complete();
                return dtSchema;

            }
        }

        private void addServiceBroker(Database db, DataTable dtSchema)
        {
            foreach (ServiceQueue q in db.ServiceBroker.Queues)
            {
                if (!q.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(q.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = q.Name;
                    r["SchemaName"] = q.Schema;
                    r["ObjectType"] = "SQ";
                    r["object_id"] = q.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = q.CreateDate;
                    r["ObjectDateModified"] = q.DateLastModified;
                    dtSchema.Rows.Add(r);
                }
            }
            foreach (ServiceRoute rt in db.ServiceBroker.Routes)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(rt.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = rt.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SBR";
                r["object_id"] = rt.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
            foreach (MessageType m in db.ServiceBroker.MessageTypes)
            {
                if (!m.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(m.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = m.Name;
                    r["SchemaName"] = "";
                    r["ObjectType"] = "SBM";
                    r["object_id"] = m.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = "1900-01-01";
                    r["ObjectDateModified"] = "1900-01-01";
                    dtSchema.Rows.Add(r);
                }
            }
            foreach (BrokerService s in db.ServiceBroker.Services)
            {
                if (!s.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(s.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = s.Name;
                    r["SchemaName"] = s.QueueSchema;
                    r["ObjectType"] = "SBS";
                    r["object_id"] = s.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = "1900-01-01";
                    r["ObjectDateModified"] = "1900-01-01";
                    dtSchema.Rows.Add(r);
                }
            }
            foreach (ServiceContract c in db.ServiceBroker.ServiceContracts)
            {
                if (!c.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(c.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = c.Name;
                    r["SchemaName"] = "";
                    r["ObjectType"] = "SBC";
                    r["object_id"] = c.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = "1900-01-01";
                    r["ObjectDateModified"] = "1900-01-01";
                    dtSchema.Rows.Add(r);
                }
            }
            foreach (RemoteServiceBinding b in db.ServiceBroker.RemoteServiceBindings)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(b.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = b.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SBB";
                r["object_id"] = b.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
            foreach (BrokerPriority p in db.ServiceBroker.Priorities)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(p.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = p.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SBP";
                r["object_id"] = p.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
        }

        private void addSeq(Database db, DataTable dtSchema)
        {
            if (db.ServerVersion.Major > 11) // 2012+
            {
                foreach (Sequence s in db.Sequences)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(s.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = s.Name;
                    r["SchemaName"] = s.Schema;
                    r["ObjectType"] = "SO";
                    r["object_id"] = s.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = s.CreateDate;
                    r["ObjectDateModified"] = s.DateLastModified;
                    dtSchema.Rows.Add(r);
                }
            }
        }


        private void addAppRole(Database db, DataTable dtSchema)
        {
            foreach (ApplicationRole ar in db.ApplicationRoles)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(ar.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = ar.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "ARO";
                r["object_id"] = ar.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = ar.CreateDate;
                r["ObjectDateModified"] = ar.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void addUsers(Database db, DataTable dtSchema)
        {
            foreach (User u in db.Users)
            {
                if (!u.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(u.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = u.Name;
                    r["SchemaName"] = "";
                    r["ObjectType"] = "USR";
                    r["object_id"] = u.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = u.CreateDate;
                    r["ObjectDateModified"] = u.DateLastModified;
                    dtSchema.Rows.Add(r);
                }
            }
        }

        private void addRoles(Database db, DataTable dtSchema)
        {
            foreach(DatabaseRole dbr in db.Roles)
            {
                if (!dbr.IsFixedRole)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(dbr.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = dbr.Name;
                    r["SchemaName"] = "";
                    r["ObjectType"] = "ROL";
                    r["object_id"] = dbr.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = dbr.CreateDate;
                    r["ObjectDateModified"] = dbr.DateLastModified;
                    dtSchema.Rows.Add(r);
                }
            }
        }

        private void addAggregate(Database db, DataTable dtSchema)
        {
            foreach (UserDefinedAggregate a in db.UserDefinedAggregates)
            {  
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(a.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = a.Name;
                r["SchemaName"] = a.Schema;
                r["ObjectType"] = "AF";
                r["object_id"] = a.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = a.CreateDate;
                r["ObjectDateModified"] = a.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void addAssembly(Database db, DataTable dtSchema)
        {
            foreach (SqlAssembly a in db.Assemblies)
            {
                if (!a.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(a.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = a.Name;
                    r["SchemaName"] = "";
                    r["ObjectType"] = "CLR";
                    r["object_id"] = a.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = a.CreateDate;
                    r["ObjectDateModified"] = DBNull.Value;
                    dtSchema.Rows.Add(r);
                }
            }
        }

        private void addXMLSchema(Database db, DataTable dtSchema)
        {
            foreach (XmlSchemaCollection x in db.XmlSchemaCollections)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(x.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = x.Name;
                r["SchemaName"] = x.Schema;
                r["ObjectType"] = "XSC";
                r["object_id"] = x.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = x.CreateDate;
                r["ObjectDateModified"] = x.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void addSchema(Database db, DataTable dtSchema)
        {
            foreach (Schema s in db.Schemas)
            {
                if (!s.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(s.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = s.Name;
                    r["SchemaName"] = "";
                    r["ObjectType"] = "SCH";
                    r["object_id"] = s.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = DBNull.Value;
                    r["ObjectDateModified"] = DBNull.Value;
                    dtSchema.Rows.Add(r);
                }
            }
        }

        private void addSynonyms(Database db, DataTable dtSchema)
        {

            foreach (Synonym s in db.Synonyms)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(s.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = s.Name;
                r["SchemaName"] = s.Schema;
                r["ObjectType"] = "SN";
                r["object_id"] = s.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = s.CreateDate;
                r["ObjectDateModified"] = s.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void addDDLTriggers(Database db, DataTable dtSchema)
        {

            foreach (DatabaseDdlTrigger t in db.Triggers)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "DTR";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = t.CreateDate;
                r["ObjectDateModified"] = t.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void addUserDefinedTableType(Database db, DataTable dtSchema)
        {

            db.PrefetchObjects(typeof(UserDefinedTableType), ScriptingOptions);
            foreach (UserDefinedTableType t in db.UserDefinedTableTypes)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = t.Schema;
                r["ObjectType"] = "TT";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = t.CreateDate;
                r["ObjectDateModified"] = t.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void addUserDefinedType(Database db, DataTable dtSchema)
        {
            foreach (UserDefinedType t in db.UserDefinedTypes)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = t.Schema;
                r["ObjectType"] = "UTY";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = DBNull.Value;
                r["ObjectDateModified"] = DBNull.Value;
                dtSchema.Rows.Add(r);
            }
        }

        private void addUserDefinedDataType(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(UserDefinedType), ScriptingOptions);
            foreach (UserDefinedDataType t in db.UserDefinedDataTypes)
            {
                var r = dtSchema.NewRow();
                var sDDL = stringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = t.Schema;
                r["ObjectType"] = "TYP";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = computeHash(bDDL);
                r["ObjectDateCreated"] = DBNull.Value;
                r["ObjectDateModified"] = DBNull.Value;
                dtSchema.Rows.Add(r);
            }
        }

        private void addUDFs(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(UserDefinedFunction), ScriptingOptions);
            foreach (UserDefinedFunction f in db.UserDefinedFunctions)
            {
                if (!f.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = f.TextHeader + Environment.NewLine + f.TextBody;
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = f.Name;
                    r["SchemaName"] = f.Schema;
                    switch (f.FunctionType)
                    {
                        case UserDefinedFunctionType.Inline:
                            r["ObjectType"] = "IF";
                            break;
                        case UserDefinedFunctionType.Scalar:
                            if (f.ImplementationType == ImplementationType.SqlClr)
                            {
                                r["ObjectType"] = "FS";
                            }
                            else
                            {
                                r["ObjectType"] = "FN";
                            }
                            break;
                        case UserDefinedFunctionType.Table:
                            if (f.ImplementationType == ImplementationType.SqlClr)
                            {
                                r["ObjectType"] = "FT";
                            }
                            else
                            {
                                r["ObjectType"] = "TF";
                            }
                            break;
                        default:
                            r["ObjectType"] = "??";
                            break;
                    }

                    r["object_id"] = f.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = f.CreateDate;
                    r["ObjectDateModified"] = f.DateLastModified;
                    dtSchema.Rows.Add(r);
                }
            }
        }

        private void addViews(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(View), ScriptingOptions);
            foreach (View v in db.Views)
            {
                if (!v.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(v.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = v.Name;
                    r["SchemaName"] = v.Schema;
                    r["ObjectType"] = "V";
                    r["object_id"] = v.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = v.CreateDate;
                    r["ObjectDateModified"] = v.DateLastModified;
                    dtSchema.Rows.Add(r);

                    addTriggers(v.Triggers, v.Schema, dtSchema);
                }
            }
        }

        private void addSPs(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(StoredProcedure), ScriptingOptions);

            foreach (StoredProcedure sp in db.StoredProcedures)
            {

                if (!sp.IsSystemObject)
                {

                    var r = dtSchema.NewRow();
                    var sDDL = sp.TextHeader + Environment.NewLine + sp.TextBody;
                    if (sp.ImplementationType == ImplementationType.SqlClr)
                    {
                        r["ObjectType"] = "PC";
                    }
                    else
                    {
                        r["ObjectType"] = "P";
                    }
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = sp.Name;
                    r["SchemaName"] = sp.Schema;
                    r["object_id"] = sp.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = sp.CreateDate;
                    r["ObjectDateModified"] = sp.DateLastModified;
                    dtSchema.Rows.Add(r);
                }
            }
        }

        private void addTables(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(Table), ScriptingOptions);
            foreach (Table t in db.Tables)
            {
                if (!t.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var sDDL = stringCollectionToString(t.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = t.Name;
                    r["SchemaName"] = t.Schema;
                    r["ObjectType"] = "U";
                    r["object_id"] = t.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = computeHash(bDDL);
                    r["ObjectDateCreated"] = t.CreateDate;
                    r["ObjectDateModified"] = t.DateLastModified;
                    dtSchema.Rows.Add(r);

                    addTriggers(t.Triggers, t.Schema, dtSchema);
                }
            }
        }

        private void addTriggers(TriggerCollection triggers,string schema,DataTable dtSchema)
        {
            if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Trigger))
            {
                foreach (Trigger t in triggers)
                {
                    if (!t.IsSystemObject)
                    {
                        var r = dtSchema.NewRow();
                        var sDDL = stringCollectionToString(t.Script(ScriptingOptions));
                        var bDDL = Zip(sDDL);
                        r["ObjectName"] = t.Name;
                        r["SchemaName"] = schema;
                        r["ObjectType"] = t.ImplementationType == ImplementationType.SqlClr ? "TA" : "TR";
                        r["object_id"] = t.ID;
                        r["DDL"] = bDDL;
                        r["DDLHash"] = computeHash(bDDL);
                        r["ObjectDateCreated"] = t.CreateDate;
                        r["ObjectDateModified"] = t.DateLastModified;
                        dtSchema.Rows.Add(r);
                    }
                }
            }
        }

 
        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.Unicode.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return Encoding.Unicode.GetString(mso.ToArray());
            }
        }

    }
}
