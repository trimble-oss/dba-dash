using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DBAChecks
{
    public class SchemaSnapshotDB
    {

        private string _connectionString;
        private SHA256Managed crypt = new SHA256Managed();

        public ScriptingOptions ScriptingOptions = new ScriptingOptions { AllowSystemObjects = false, IncludeHeaders = false, DriAll = true, Triggers = true, FullTextIndexes = true, Indexes = true, XmlIndexes = true, ExtendedProperties = true, Statistics = true };

        public SchemaSnapshotDB(string connectionString)
        {
            _connectionString = connectionString;
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
                addAggregate(db, dtSchema);
                addAssembly(db, dtSchema);
                addUserDefinedType(db, dtSchema);
                addXMLSchema(db, dtSchema);
                addSchema(db, dtSchema);
                addTables(db, dtSchema);
                addSPs(db, dtSchema);
                addUDFs(db, dtSchema);
                addViews(db, dtSchema);
                addUserDefinedTableType(db, dtSchema);
                addUserDefinedDataType(db, dtSchema);
                addDDLTriggers(db, dtSchema);
                addSynonyms(db, dtSchema);

                // break;
            }
            return dtSchema;
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
