using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Broker;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using SerilogTimings;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

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
        public bool DriAll = true;
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
            return new[] { SchemaSnapshotDBObjectTypes.Database, SchemaSnapshotDBObjectTypes.Aggregate, SchemaSnapshotDBObjectTypes.Assembly, SchemaSnapshotDBObjectTypes.DDLTrigger, SchemaSnapshotDBObjectTypes.Schema, SchemaSnapshotDBObjectTypes.StoredProcedures, SchemaSnapshotDBObjectTypes.Synonym, SchemaSnapshotDBObjectTypes.Tables, SchemaSnapshotDBObjectTypes.UserDefinedDataType, SchemaSnapshotDBObjectTypes.UserDefinedFunction, SchemaSnapshotDBObjectTypes.UserDefinedTableType, SchemaSnapshotDBObjectTypes.UserDefinedType, SchemaSnapshotDBObjectTypes.View, SchemaSnapshotDBObjectTypes.XMLSchema, SchemaSnapshotDBObjectTypes.Roles, SchemaSnapshotDBObjectTypes.ApplicationRole, SchemaSnapshotDBObjectTypes.Sequence, SchemaSnapshotDBObjectTypes.ServiceBroker, SchemaSnapshotDBObjectTypes.Trigger };
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

    public class SchemaSnapshotDB : SMOBaseClass
    {
        public SchemaSnapshotDB(DBADashConnection source, SchemaSnapshotDBOptions options) : base(source, options)
        {
        }

        public SchemaSnapshotDB(DBADashConnection source) : base(source)
        {
        }

        private const string ErrorSource = "SchemaSnapshot";

        private static DataTable RgDTSchema()
        {
            DataTable dtRG = new()
            {
                TableName = "ResourceGovernorConfiguration"
            };
            dtRG.Columns.Add("is_enabled", typeof(bool));
            dtRG.Columns.Add("classifier_function", typeof(string));
            dtRG.Columns.Add("reconfiguration_error", typeof(bool));
            dtRG.Columns.Add("reconfiguration_pending", typeof(bool));
            dtRG.Columns.Add("max_outstanding_io_per_volume", typeof(int));
            dtRG.Columns.Add("script", typeof(string));
            return dtRG;
        }

        public DataTable ResourceGovernorConfiguration()
        {
            var dtRG = RgDTSchema();
            var reConfigError = false;
            using (var cn = new SqlConnection(MasterConnectionString))
            {
                var instance = new Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
                if (instance.EngineEdition != Edition.EnterpriseOrDeveloper) return dtRG; //  RG is an enterprise only edition feature
                var rg = instance.ResourceGovernor;
                // Script RG configuration
                var sc = rg.Script();
                // Add classifier function script
                sc.Insert(0, ObjectDDL(MasterConnectionString, rg.ClassifierFunction));

                // Script out resource pool configuration and workload groups
                foreach (ResourcePool pool in rg.ResourcePools)
                {
                    if (pool.IsSystemObject && pool.ID == 1) continue; // Ignore internal pool which can't be configured
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
                        if (ex.InnerException?.Message == "The resource governor resource pool information is not complete. This can happen if a pool was created but the resource governor is not reconfigured.")
                        {
                            // Might have an issue scripting if a pool is created without running ALTER RESOURCE GOVERNOR RECONFIGURE.  Ignore this error and add a comment to the script
                            sc.Add($"/* Unable to script pool {pool.Name} until resource governor is reconfigured */");
                            reConfigError = true;
                        }
                        else
                        {
                            throw;
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
                row["reconfiguration_error"] = reConfigError;
                row["reconfiguration_pending"] = rg.ReconfigurePending;
                row["max_outstanding_io_per_volume"] = rg.ServerVersion.Major >= 12 ? rg.MaxOutstandingIOPerVolume : 0; // Supported 2014 and later
                row["script"] = StringCollectionToString(sc);
                dtRG.Rows.Add(row);
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

        public DataTable SnapshotDB(string DBName, IErrorLogger errorLogger)
        {
            var StartTime = DateTime.UtcNow;

            using (var cn = new SqlConnection(ConnectionString))
            using (var opSS = Operation.Begin("Schema snapshot {dbName}", DBName))
            {
                DataTable dtSchema = new("Schema_" + DBName);
                dtSchema.Columns.Add("ObjectName");
                dtSchema.Columns.Add("SchemaName");
                dtSchema.Columns.Add("ObjectType");
                dtSchema.Columns.Add("object_id", typeof(int));
                dtSchema.Columns.Add("DDLHash", typeof(byte[]));
                dtSchema.Columns.Add("DDL", typeof(byte[]));
                dtSchema.Columns.Add("ObjectDateCreated", typeof(DateTime));
                dtSchema.Columns.Add("ObjectDateModified", typeof(DateTime));
                var instance = new Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
                instance.ConnectionContext.StatementTimeout = CollectionType.SchemaSnapshot.GetCommandTimeout();
                instance.SetDefaultInitFields(typeof(Table), true);

                var db = instance.Databases[DBName];
                if (db.IsUpdateable && db.IsAccessible)
                {
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Database))
                    {
                        var sDDL = StringCollectionToString(db.Script(ScriptingOptions));
                        var bDDL = Zip(sDDL);
                        var r = dtSchema.NewRow();
                        r["ObjectName"] = "Database";
                        r["SchemaName"] = "";
                        r["ObjectType"] = "DB";
                        r["object_id"] = 0;
                        r["DDL"] = bDDL;
                        r["DDLHash"] = ComputeHash(bDDL);
                        r["ObjectDateCreated"] = db.CreateDate;
                        r["ObjectDateModified"] = DBNull.Value;
                        dtSchema.Rows.Add(r);
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Aggregate))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Aggregate"))
                        {
                            AddAggregate(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Assembly))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Assembly"))
                        {
                            AddAssembly(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedType))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "UserDefinedType"))
                        {
                            AddUserDefinedType(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.XMLSchema))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "XMLSchema"))
                        {
                            AddXMLSchema(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Schema))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Schema"))
                        {
                            AddSchema(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Tables))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Tables"))
                        {
                            AddTables(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.StoredProcedures))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "StoredProcedures"))
                        {
                            AddSPs(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedFunction))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "UserDefinedFunctions"))
                        {
                            AddUDFs(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.View))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Views"))
                        {
                            AddViews(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedTableType) && instance.VersionMajor >= 10) // Support for User Defined Table Types added in SQL 2008
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "UserDefinedTableTypes"))
                        {
                            AddUserDefinedTableType(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedDataType))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "UserDefinedDataTypes"))
                        {
                            AddUserDefinedDataType(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.DDLTrigger))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "DDLTriggers"))
                        {
                            AddDDLTriggers(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Synonym))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Synonyms"))
                        {
                            AddSynonyms(db, dtSchema, errorLogger);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Roles))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Roles"))
                        {
                            AddRoles(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Users))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Users"))
                        {
                            AddUsers(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.ApplicationRole))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "ApplicationRoles"))
                        {
                            AddAppRole(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Sequence))
                    {
                        using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "Sequences"))
                        {
                            AddSeq(db, dtSchema);
                            op.Complete();
                        }
                    }
                    if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.ServiceBroker))
                    {
                        if (instance.ServerType != Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase)
                        {
                            using (var op = Operation.Begin("Schema snapshot {dbName}: {Object}", DBName, "ServiceBroker"))
                            {
                                AddServiceBroker(db, dtSchema);
                                op.Complete();
                            }
                        }
                    }
                }
                opSS.Complete();
                var EndTime = DateTime.UtcNow;
                dtSchema.TableName = "Snapshot_" + DBName;
                dtSchema.ExtendedProperties.Add("StartTimeBin", StartTime.ToBinary().ToString());
                dtSchema.ExtendedProperties.Add("EndTimeBin", EndTime.ToBinary().ToString());
                dtSchema.ExtendedProperties.Add("SnapshotOptions", JsonConvert.SerializeObject(options));
                return dtSchema;
            }
        }

        private void AddServiceBroker(Database db, DataTable dtSchema)
        {
            foreach (ServiceQueue q in db.ServiceBroker.Queues)
            {
                if (q.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(q.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = q.Name;
                r["SchemaName"] = q.Schema;
                r["ObjectType"] = "SQ";
                r["object_id"] = q.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = q.CreateDate;
                r["ObjectDateModified"] = q.DateLastModified;
                dtSchema.Rows.Add(r);
            }
            foreach (ServiceRoute rt in db.ServiceBroker.Routes)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(rt.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = rt.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SBR";
                r["object_id"] = rt.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
            foreach (MessageType m in db.ServiceBroker.MessageTypes)
            {
                if (m.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(m.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = m.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SBM";
                r["object_id"] = m.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
            foreach (BrokerService s in db.ServiceBroker.Services)
            {
                if (s.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(s.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = s.Name;
                r["SchemaName"] = s.QueueSchema;
                r["ObjectType"] = "SBS";
                r["object_id"] = s.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
            foreach (ServiceContract c in db.ServiceBroker.ServiceContracts)
            {
                if (c.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(c.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = c.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SBC";
                r["object_id"] = c.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
            foreach (RemoteServiceBinding b in db.ServiceBroker.RemoteServiceBindings)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(b.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = b.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SBB";
                r["object_id"] = b.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }

            if (db.Parent.VersionMajor < 10) return; // Broker priorities supported on SQL 2008

            foreach (BrokerPriority p in db.ServiceBroker.Priorities)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(p.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = p.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SBP";
                r["object_id"] = p.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
        }

        private void AddSeq(Database db, DataTable dtSchema)
        {
            if (db.ServerVersion.Major <= 11) return; // 2012+
            foreach (Sequence s in db.Sequences)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(s.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = s.Name;
                r["SchemaName"] = s.Schema;
                r["ObjectType"] = "SO";
                r["object_id"] = s.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = s.CreateDate;
                r["ObjectDateModified"] = s.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddAppRole(Database db, DataTable dtSchema)
        {
            foreach (ApplicationRole ar in db.ApplicationRoles)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(ar.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = ar.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "ARO";
                r["object_id"] = ar.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = ar.CreateDate;
                r["ObjectDateModified"] = ar.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddUsers(Database db, DataTable dtSchema)
        {
            foreach (User u in db.Users)
            {
                if (u.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(u.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = u.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "USR";
                r["object_id"] = u.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = u.CreateDate;
                r["ObjectDateModified"] = u.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddRoles(Database db, DataTable dtSchema)
        {
            foreach (DatabaseRole dbr in db.Roles)
            {
                if (dbr.IsFixedRole) continue;
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(dbr.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = dbr.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "ROL";
                r["object_id"] = dbr.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = dbr.CreateDate;
                r["ObjectDateModified"] = dbr.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddAggregate(Database db, DataTable dtSchema)
        {
            foreach (UserDefinedAggregate a in db.UserDefinedAggregates)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(a.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = a.Name;
                r["SchemaName"] = a.Schema;
                r["ObjectType"] = "AF";
                r["object_id"] = a.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = a.CreateDate;
                r["ObjectDateModified"] = a.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddAssembly(Database db, DataTable dtSchema)
        {
            foreach (SqlAssembly a in db.Assemblies)
            {
                if (a.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(a.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = a.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "CLR";
                r["object_id"] = a.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = a.CreateDate;
                r["ObjectDateModified"] = DBNull.Value;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddXMLSchema(Database db, DataTable dtSchema)
        {
            foreach (XmlSchemaCollection x in db.XmlSchemaCollections)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(x.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = x.Name;
                r["SchemaName"] = x.Schema;
                r["ObjectType"] = "XSC";
                r["object_id"] = x.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = x.CreateDate;
                r["ObjectDateModified"] = x.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddSchema(Database db, DataTable dtSchema)
        {
            foreach (Schema s in db.Schemas)
            {
                if (s.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(s.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = s.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "SCH";
                r["object_id"] = s.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = DBNull.Value;
                r["ObjectDateModified"] = DBNull.Value;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddSynonyms(Database db, DataTable dtSchema, IErrorLogger logger)
        {
            foreach (Synonym s in db.Synonyms)
            {
                try
                {
                    var r = dtSchema.NewRow();
                    var sDDL = StringCollectionToString(s.Script(ScriptingOptions));
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = s.Name;
                    r["SchemaName"] = s.Schema;
                    r["ObjectType"] = "SN";
                    r["object_id"] = s.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = ComputeHash(bDDL);
                    r["ObjectDateCreated"] = s.CreateDate;
                    r["ObjectDateModified"] = s.DateLastModified;
                    dtSchema.Rows.Add(r);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ErrorSource, db.Name);
                }
            }
        }

        private void AddDDLTriggers(Database db, DataTable dtSchema)
        {
            foreach (DatabaseDdlTrigger t in db.Triggers)
            {
                var r = dtSchema.NewRow();
                var sDDL = t.IsEncrypted ? "/* Encrypted DDL Trigger */" : StringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = "";
                r["ObjectType"] = "DTR";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = t.CreateDate;
                r["ObjectDateModified"] = t.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddUserDefinedTableType(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(UserDefinedTableType), ScriptingOptions);
            foreach (UserDefinedTableType t in db.UserDefinedTableTypes)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = t.Schema;
                r["ObjectType"] = "TT";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = t.CreateDate;
                r["ObjectDateModified"] = t.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddUserDefinedType(Database db, DataTable dtSchema)
        {
            foreach (UserDefinedType t in db.UserDefinedTypes)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = t.Schema;
                r["ObjectType"] = "UTY";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = DBNull.Value;
                r["ObjectDateModified"] = DBNull.Value;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddUserDefinedDataType(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(UserDefinedType), ScriptingOptions);
            foreach (UserDefinedDataType t in db.UserDefinedDataTypes)
            {
                var r = dtSchema.NewRow();
                var sDDL = StringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = t.Schema;
                r["ObjectType"] = "TYP";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = DBNull.Value;
                r["ObjectDateModified"] = DBNull.Value;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddUDFs(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(UserDefinedFunction), ScriptingOptions);
            foreach (UserDefinedFunction f in db.UserDefinedFunctions)
            {
                if (f.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                string sDDL;
                if (f.IsEncrypted)
                {
                    sDDL = "/* Encrypted Function */";
                }
                else
                {
                    sDDL = f.TextHeader + Environment.NewLine + f.TextBody;
                }

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
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = f.CreateDate;
                r["ObjectDateModified"] = f.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddViews(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(View), ScriptingOptions);
            foreach (View v in db.Views)
            {
                if (v.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = v.IsEncrypted ? "/* Encrypted View */" : StringCollectionToString(v.Script(ScriptingOptions));

                var bDDL = Zip(sDDL);
                r["ObjectName"] = v.Name;
                r["SchemaName"] = v.Schema;
                r["ObjectType"] = "V";
                r["object_id"] = v.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = v.CreateDate;
                r["ObjectDateModified"] = v.DateLastModified;
                dtSchema.Rows.Add(r);

                AddTriggers(v.Triggers, v.Schema, dtSchema);
            }
        }

        private void AddSPs(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(StoredProcedure), ScriptingOptions);

            foreach (StoredProcedure sp in db.StoredProcedures)
            {
                if (sp.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                string sDDL;
                if (sp.IsEncrypted)
                {
                    sDDL = "/* Encrypted stored procedure */";
                }
                else
                {
                    sDDL = sp.TextHeader + Environment.NewLine + sp.TextBody;
                }
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
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = sp.CreateDate;
                r["ObjectDateModified"] = sp.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        private void AddTables(Database db, DataTable dtSchema)
        {
            db.PrefetchObjects(typeof(Table), ScriptingOptions);
            var includeTriggers = options.Triggers;
            foreach (Table t in db.Tables)
            {
                if (!t.IsSystemObject)
                {
                    var r = dtSchema.NewRow();
                    var hasEncryptedTriggers = false;
                    // Don't script triggers if table contains encrypted triggers
                    if (includeTriggers)
                    {
                        foreach (Trigger trigger in t.Triggers)
                        {
                            if (trigger.IsEncrypted)
                            {
                                hasEncryptedTriggers = true;
                                ScriptingOptions.Triggers = false;
                                break;
                            }
                        }
                    }
                    var sDDL = StringCollectionToString(t.Script(ScriptingOptions));
                    if (hasEncryptedTriggers)
                    {
                        sDDL += Environment.NewLine + "/* Encrypted Triggers */";
                    }
                    var bDDL = Zip(sDDL);
                    r["ObjectName"] = t.Name;
                    r["SchemaName"] = t.Schema;
                    r["ObjectType"] = "U";
                    r["object_id"] = t.ID;
                    r["DDL"] = bDDL;
                    r["DDLHash"] = ComputeHash(bDDL);
                    r["ObjectDateCreated"] = t.CreateDate;
                    r["ObjectDateModified"] = t.DateLastModified;
                    dtSchema.Rows.Add(r);

                    AddTriggers(t.Triggers, t.Schema, dtSchema);
                }
                ScriptingOptions.Triggers = includeTriggers;
            }
        }

        private void AddTriggers(TriggerCollection triggers, string schema, DataTable dtSchema)
        {
            if (!options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Trigger)) return;
            foreach (Trigger t in triggers)
            {
                if (t.IsSystemObject) continue;
                var r = dtSchema.NewRow();
                var sDDL = t.IsEncrypted ? "/* Encrypted trigger */" : StringCollectionToString(t.Script(ScriptingOptions));
                var bDDL = Zip(sDDL);
                r["ObjectName"] = t.Name;
                r["SchemaName"] = schema;
                r["ObjectType"] = t.ImplementationType == ImplementationType.SqlClr ? "TA" : "TR";
                r["object_id"] = t.ID;
                r["DDL"] = bDDL;
                r["DDLHash"] = ComputeHash(bDDL);
                r["ObjectDateCreated"] = t.CreateDate;
                r["ObjectDateModified"] = t.DateLastModified;
                dtSchema.Rows.Add(r);
            }
        }

        public static async Task GenerateSchemaSnapshots(CollectionConfig Config, DBADashSource src)
        {
            var connectionString = src.GetSource();
            SqlConnectionStringBuilder builder = new(connectionString);

            var collector = await DBCollector.CreateAsync(src, Config.ServiceName);
            var dsSnapshot = collector.Data;
            var dbs = src.SchemaSnapshotDBs.Split(',');

            await using var cn = new SqlConnection(connectionString);
            var ss = new SchemaSnapshotDB(src.SourceConnection, Config.SchemaSnapshotOptions);
            var instance = new Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));

            if (instance.ServerType == Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase && builder.InitialCatalog is null or "master" or "")
            {
                return;
            }
            Log.Information("DB Snapshots from instance {source}", src.SourceConnection.ConnectionForPrint);

            foreach (Database db in instance.Databases)
            {
                if (!db.IsUpdateable || !db.IsAccessible || db.IsSystemObject) continue;

                var include = dbs.Any(strDB => strDB == "*" || string.Equals(strDB, db.Name, StringComparison.OrdinalIgnoreCase)) &&
                              !dbs.Any(strDB => strDB.StartsWith('-') && string.Equals(strDB[1..], db.Name, StringComparison.OrdinalIgnoreCase));
                if (!include) continue;

                Log.Information("DB Snapshot {db} from instance {instance}", db.Name, builder.DataSource);
                DataTable dt = null;
                try
                {
                    dt = ss.SnapshotDB(db.Name, collector);
                    dsSnapshot.Tables.Add(dt);
                }
                catch (Exception ex)
                {
                    collector.LogError(ex, "Error creating schema snapshot", db.Name);
                }
                try
                {
                    var fileName = DBADashSource.GenerateFileName(src.SourceConnection.ConnectionForFileName);
                    await DestinationHandling.WriteAllDestinationsAsync(dsSnapshot, src, fileName, Config);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error writing schema snapshot {db} to destination", db.Name);
                }
                if (dt != null)
                {
                    dsSnapshot.Tables.Remove(dt);
                }
                collector.ClearErrors();
            }
        }
    }
}