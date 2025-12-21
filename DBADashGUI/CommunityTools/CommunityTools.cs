using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI.CommunityTools
{
    internal static class CommunityTools
    {
        public const string FirstResponderKitUrl = "https://github.com/BrentOzarULTD/SQL-Server-First-Responder-Kit";
        public const string ErikDarlingUrl = "https://github.com/erikdarlingdata/DarlingData";

        public static readonly Dictionary<object, string> BooleanPickerItems = new()
        {
            { true, "Yes" },
            { false, "No" },
        };

        public static List<DirectExecutionReport> CommunityToolsList = new()
        {
            sp_LogHunter.Instance,
            sp_WhoIsActive.Instance,
            sp_Blitz.Instance,
            sp_BlitzWho.Instance,
            sp_BlitzIndex.Instance,
            sp_BlitzCache.Instance,
            sp_BlitzLock.Instance,
            sp_BlitzFirst.Instance,
            sp_BlitzBackups.Instance,
            sp_HumanEvents.Instance,
            sp_PressureDetector.Instance,
            sp_HealthParser.Instance,
            sp_QuickieStore.Instance,
            sp_HumanEventsBlockViewer.Instance,
            sp_SrvPermissions.Instance,
            sp_DBPermissions.Instance,
            sp_IndexCleanup.Instance
        };

        public static List<DirectExecutionReport> DatabaseLevelCommunityTools =>
            CommunityToolsList.Where(x => x.DatabaseNameParameter != null).ToList();

        public static List<DirectExecutionReport> ProcedureLevelTools = new()
        {
            sp_BlitzCache.Instance,
            sp_DBPermissions.Instance,
            sp_HumanEvents.Instance,
            sp_HumanEventsBlockViewer.Instance,
            sp_QuickieStore.Instance
        };

        public static List<DirectExecutionReport> TableLevelTools = new()
        {
            sp_BlitzIndex.Instance,
            sp_IndexCleanup.Instance,
            sp_DBPermissions.Instance,
        };
    }
}