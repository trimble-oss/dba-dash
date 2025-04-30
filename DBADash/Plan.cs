using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DBADash
{
    public class Plan : IEqualityComparer<Plan>
    {
        public readonly byte[] PlanHandle;
        public readonly int StartOffset;
        public readonly int EndOffset;
        public readonly byte[] PlanHash;
        public readonly string Key;

        public Plan(byte[] planHandle, byte[] planHash, int startOffset, int endOffset)
        {
            PlanHandle = planHandle;
            PlanHash = planHash;
            StartOffset = startOffset;
            EndOffset = endOffset;
            Key = Convert.ToBase64String(PlanHandle.Concat(PlanHash).Concat(BitConverter.GetBytes(StartOffset)).Concat(BitConverter.GetBytes(EndOffset)).ToArray());
        }

        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || GetType() != obj.GetType())
            {
                return false;
            }
            else
            {
                var p = (Plan)obj;
                return p.Key == Key;
            }
        }

        public bool Equals(Plan x, Plan y)
        {
            return x?.Key == y?.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public int GetHashCode(Plan obj)
        {
            return obj.Key.GetHashCode();
        }

        public static async Task<DataTable> GetPlansAsync(List<Plan> plansToCollect, string ConnectionString)
        {
            var plansSQL = GetPlansSQL(plansToCollect);
            if (string.IsNullOrEmpty(plansSQL)) throw new Exception("Plan collection SQL was null/empty");

            await using var cn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(plansSQL, cn);
            var dt = new DataTable("QueryPlans");
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            dt.Load(rdr);
            if (dt is not { Rows.Count: > 0 }) return dt;
            dt.Columns.Add("query_plan_hash", typeof(byte[]));
            dt.Columns.Add("query_plan_compressed", typeof(byte[]));
            foreach (DataRow r in dt.Rows)
            {
                try
                {
                    var strPlan = r["query_plan"] == DBNull.Value ? string.Empty : (string)r["query_plan"];
                    r["query_plan_compressed"] = SMOBaseClass.Zip(strPlan);
                    var hash = GetPlanHash(strPlan);
                    r["query_plan_hash"] = hash;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing query plans");
                }
            }

            dt.Columns.Remove("query_plan");

            var nullRows = dt.Select("query_plan_hash IS NULL");

            // Remove rows with null query plan hash
            if (nullRows.Length > 0)
            {
                Log.Information("Removing {0} rows with NULL query_plan_hash", nullRows.Length);
                foreach (var row in nullRows)
                {
                    row.Delete();
                }

                dt.AcceptChanges();
            }

            return dt;
        }

        ///<summary>
        ///Generate a SQL query to get the query plan text for running queries. Captured plan handles get cached with a call to CacheCollectedPlans later <br/>
        ///Limits the cost associated with plan capture - less plans to capture, send and process<br/>
        ///Note: Caching takes query_plan_hash into account as a statement can get recompiled without the plan handle changing.
        ///</summary>
        private static string GetPlansSQL(List<Plan> plansToCollect)
        {
            var sb = new StringBuilder();
            sb.Append(@"DECLARE @plans TABLE(plan_handle VARBINARY(64),statement_start_offset int,statement_end_offset int)
INSERT INTO @plans(plan_handle,statement_start_offset,statement_end_offset)
VALUES");

            plansToCollect.ForEach(p => sb.AppendFormat("{3}(0x{0},{1},{2}),", p.PlanHandle.ToHexString(), p.StartOffset, p.EndOffset, Environment.NewLine));
            if (plansToCollect.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine("OPTION(RECOMPILE)"); // Plan caching is not beneficial.  RECOMPILE hint to avoid polluting the plan cache
                sb.AppendLine();
                sb.Append(@"SELECT t.plan_handle,
        t.statement_start_offset,
        t.statement_end_offset,
        pln.dbid,
        pln.objectid,
        pln.encrypted,
        pln.query_plan
FROM @plans t
CROSS APPLY sys.dm_exec_text_query_plan(t.plan_handle,t.statement_start_offset,t.statement_end_offset) pln
OPTION(RECOMPILE)"); // Plan caching is not beneficial.  RECOMPILE hint to avoid polluting the plan cache
                return sb.ToString();
            }
        }

        ///<summary>
        ///Get the query plan hash from a  string of the plan XML
        ///</summary>
        public static byte[] GetPlanHash(string strPlan)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(strPlan));
            ms.Position = 0;
            using var xr = new XmlTextReader(ms);
            while (xr.Read())
            {
                if (xr.Name != "StmtSimple") continue;
                var strHash = xr.GetAttribute("QueryPlanHash");
                return strHash.ToByteArray();
            }

            return Array.Empty<byte>();
        }
    }
}