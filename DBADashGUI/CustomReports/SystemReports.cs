using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    public class SystemReports : List<CustomReport>
    {
        public SystemReports()
        {
            Add(DatabaseExtendedPropertiesReport.Instance);
            Add(DatabaseFinderReport.Instance);
            Add(DeadlockChartsReport.Instance);
            Add(DeadlocksReport.Instance);
            Add(DeletedDatabasesReport.Instance);
            Add(FailedLoginsReport.Instance);
            Add(FlushPlanLogReport.Instance);
            Add(KillSessionLogReport.Instance);
            Add(NewDatabasesReport.Instance);
            Add(QueryStatsChartsReport.Instance);
            Add(QueryStatsReport.Instance);
            Add(ServerRoleMembersReport.Instance);
            Add(ServerServicesReport.Instance);
            Add(SQLLoginPasswordAgeReport.Instance);
            Add(TableSizeHistoryReport.Instance);
            Add(TableSizeReport.Instance);
        }
    }
}