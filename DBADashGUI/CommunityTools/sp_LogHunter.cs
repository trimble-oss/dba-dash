using DBADash.Messaging;
using DBADashGUI.CustomReports;
using System.Collections.Generic;

namespace DBADashGUI.CommunityTools
{
    internal class sp_LogHunter
    {
        public static DirectExecutionReport Instance = new()
        {
            ProcedureName = ProcedureExecutionMessage.CommunityProcs.sp_LogHunter.ToString(),
            ReportName = ProcedureExecutionMessage.CommunityProcs.sp_LogHunter.ToString(),
            URL = CommunityTools.ErikDarlingUrl,
            Description = "Search ErrorLog",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@days_back", ParamType = "INT" },
                    new() { ParamName = "@custom_message", ParamType = "NVARCHAR" },
                    new() { ParamName = "@custom_message_only", ParamType = "BIT" },
                    new() { ParamName = "@first_log_only", ParamType = "BIT" },
                    new() { ParamName = "@start_date", ParamType = "DATETIME" },
                    new() { ParamName = "@end_date", ParamType = "DATETIME" },
                    new() { ParamName = "@language_id", ParamType = "INT" },
                }
            }
        };
    }
}