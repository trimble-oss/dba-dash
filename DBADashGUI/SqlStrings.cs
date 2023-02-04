using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    internal class SqlStrings
    {
        public static string GetSqlString(string name)
        {
            var resourcePath = "DBADashGUI.SQL." + name + ".sql";
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null) return string.Empty;
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private static string FindPlan => GetSqlString("FindPlan");

        private static string DecipherWaitResource => GetSqlString("DecipherWaitResource");

        public static string GetFindPlan(string queryPlanHash, string queryHash, string planHandle, string sqlHandle, string db, int statementStartOffset, int statementEndOffset, string instance)
        {
            if (string.IsNullOrEmpty(planHandle))
            {
                planHandle = "0x /* WARNING: plan handle was not available for this query */";
            }

            if (string.IsNullOrEmpty(sqlHandle))
            {
                sqlHandle = "0x /* WARNING: sql handle was not available for this query */";
            }

            if (string.IsNullOrEmpty((queryHash)))
            {
                queryHash = "0x /* WARNING: query hash was not available for this query */";
            }

            if (string.IsNullOrEmpty(queryPlanHash))
            {
                queryPlanHash = "0x /* WARNING: query plan hash was not available for this query */";
            }

            if (string.IsNullOrEmpty(db))
            {
                db = "???";
            }
            db = db.Replace("]", "]]"); // Prevent ending square bracket
            instance = instance.Replace("*", "+"); // Prevent ending of comment section with */

            return FindPlan.Replace("'{plan_handle}'", planHandle.Trim())
                .Replace("'{sql_handle}'", sqlHandle.Trim())
                .Replace("'{query_hash}'", queryHash.Trim())
                .Replace("'{query_plan_hash}'", queryPlanHash.Trim())
                .Replace("{database_name}", db.Trim())
                .Replace("'{statement_start_offset}'", statementStartOffset.ToString())
                .Replace("'{statement_end_offset}'", statementEndOffset.ToString())
                .Replace("{instance_name}", instance);
        }

        public static string GetDecipherWaitResource(string waitResource, string instance)
        {
            instance = instance.Replace("*", "+"); // Prevent ending of comment section with */
            waitResource = waitResource.Replace("'","''"); // Ensure wait resource is quoted just in case
            return DecipherWaitResource.Replace("{wait_resource}", waitResource)
                .Replace("{instance_name}", instance);
        }
    }
}