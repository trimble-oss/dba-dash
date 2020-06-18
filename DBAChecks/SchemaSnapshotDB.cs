using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Broker;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DBAChecks
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
        ServiceBroker
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
            return new SchemaSnapshotDBObjectTypes[] {SchemaSnapshotDBObjectTypes.Database, SchemaSnapshotDBObjectTypes.Aggregate, SchemaSnapshotDBObjectTypes.Assembly, SchemaSnapshotDBObjectTypes.DDLTrigger, SchemaSnapshotDBObjectTypes.Schema, SchemaSnapshotDBObjectTypes.StoredProcedures, SchemaSnapshotDBObjectTypes.Synonym, SchemaSnapshotDBObjectTypes.Tables, SchemaSnapshotDBObjectTypes.UserDefinedDataType, SchemaSnapshotDBObjectTypes.UserDefinedFunction, SchemaSnapshotDBObjectTypes.UserDefinedTableType, SchemaSnapshotDBObjectTypes.UserDefinedType, SchemaSnapshotDBObjectTypes.View, SchemaSnapshotDBObjectTypes.XMLSchema , SchemaSnapshotDBObjectTypes.Roles, SchemaSnapshotDBObjectTypes.ApplicationRole, SchemaSnapshotDBObjectTypes.Sequence, SchemaSnapshotDBObjectTypes.ServiceBroker};
        }


        public ScriptingOptions ScriptOptions()
        {
            var so = new ScriptingOptions();
            so.DriAll = DriAll;
            so.Triggers = Triggers;
            so.FullTextIndexes = FullTextIndexes;
            so.Indexes = Indexes;
            so.XmlIndexes = XMLIndexes;
            so.ExtendedProperties = ExtendedProperties;
            so.Statistics = Statistics;
            so.DriIncludeSystemNames = DriIncludeSystemNames;
            so.Permissions = Permissions;
            return so;
        }

    }

    public class SchemaSnapshotDB
    {

        private string _connectionString;
        private SHA256Managed crypt = new SHA256Managed();
        private SchemaSnapshotDBOptions options;
        private ScriptingOptions ScriptingOptions;

        public SchemaSnapshotDB(string connectionString,SchemaSnapshotDBOptions options)
        {
            _connectionString = connectionString;
            this.options = options;
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

        public DataTable SnapshotDB(string DBName)
        {

            var cn = new SqlConnection(_connectionString);

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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
                    r["ObjectDateCreated"] = db.CreateDate;
                    r["ObjectDateModified"] = DBNull.Value;
                    dtSchema.Rows.Add(r);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Aggregate))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Aggregate");
                    addAggregate(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Assembly))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Assembly");
                    addAssembly(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedType))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": UserDefinedType");
                    addUserDefinedType(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.XMLSchema))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": XMLSchema");
                    addXMLSchema(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Schema))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Schema");
                    addSchema(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Tables))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Tables");
                    addTables(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.StoredProcedures))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": StoredProcedures");
                    addSPs(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedFunction))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": UserDefinedFunctions");
                    addUDFs(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.View))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Views");
                    addViews(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedTableType))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": UserDefinedTableTypes");
                    addUserDefinedTableType(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.UserDefinedDataType))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": UserDefinedDataTypes");
                    addUserDefinedDataType(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.DDLTrigger))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": DDLTriggers");
                    addDDLTriggers(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Synonym))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Synonyms");
                    addSynonyms(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Roles))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Roles");
                    addRoles(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Users))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Users");
                    addUsers(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.ApplicationRole))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": ApplicationRoles");
                    addAppRole(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.Sequence))
                {
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Sequences");
                    addSeq(db, dtSchema);
                }
                if (options.ObjectTypes.Contains(SchemaSnapshotDBObjectTypes.ServiceBroker))
                {
                    if (instance.ServerType != Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase)
                    {
                        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": ServiceBroker");
                        addServiceBroker(db, dtSchema);
                    }
                   
                }
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " SchemaSnapshot: " + DBName + ": Complete");
                // break;
            }
            return dtSchema;
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
                r["ObjectDateCreated"] = "1900-01-01";
                r["ObjectDateModified"] = "1900-01-01";
                dtSchema.Rows.Add(r);
            }
        }

        private void addSeq(Database db, DataTable dtSchema)
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
                r["ObjectDateCreated"] = s.CreateDate;
                r["ObjectDateModified"] = s.DateLastModified;
                dtSchema.Rows.Add(r);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
                    r["ObjectDateCreated"] = v.CreateDate;
                    r["ObjectDateModified"] = v.DateLastModified;
                    dtSchema.Rows.Add(r);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
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
                    r["DDLHash"] = crypt.ComputeHash(bDDL);
                    r["ObjectDateCreated"] = t.CreateDate;
                    r["ObjectDateModified"] = t.DateLastModified;
                    dtSchema.Rows.Add(r);
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
