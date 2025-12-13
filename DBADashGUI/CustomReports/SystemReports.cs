using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    public class SystemReports : List<CustomReport>
    {
        public SystemReports()
        {
            Add(DatabaseFinderReport.Instance);
            Add(DeletedDatabasesReport.Instance);
            Add(FailedLoginsReport.Instance);
            Add(NewDatabasesReport.Instance);
            Add(ServerRoleMembersReport.Instance);
            Add(ServerServicesReport.Instance);
            Add(TableSizeHistoryReport.Instance);
            Add(TableSizeReport.Instance);
        }
    }
}