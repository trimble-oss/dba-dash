using DBADash;
using DBADashGUI.Performance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    internal class QueryStoreLinkColumnInfo : LinkColumnInfo
    {
        public enum QueryStoreLinkColumnType
        {
            QueryID,
            PlanID,
            ObjectName,
            QueryHash,
            PlanHash
        }

        public QueryStoreLinkColumnType TargetColumnLinkType { get; set; }

        public string TargetColumn { get; set; }

        public string InstanceIdColumn { get; set; }

        public string DatabaseNameColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            // A blank cell - a rollup row has no hash of its own - is not something to look up
            var value = row.Cells[TargetColumn].Value.DBNullToNull();
            if (value == null) return;
            var hash = GetHash(value);
            if (TargetColumnLinkType is (QueryStoreLinkColumnType.QueryHash or QueryStoreLinkColumnType.PlanHash) && hash == null) return;

            var frmQS = new QueryStoreViewer();

            var newContext = context.DeepCopy();
            if (!string.IsNullOrEmpty(InstanceIdColumn) && row.Cells[InstanceIdColumn].Value.DBNullToNull() is int instanceId)
            {
                newContext.InstanceID = instanceId;
            }
            if (!string.IsNullOrEmpty(DatabaseNameColumn))
            {
                // No database searches them all, which is the right answer for a query whose database is unknown
                newContext.DatabaseName = row.Cells[DatabaseNameColumn].Value.DBNullToNull() as string;
            }

            switch (TargetColumnLinkType)
            {
                case QueryStoreLinkColumnType.QueryID:
                    frmQS.QueryId = (long)value;
                    break;

                case QueryStoreLinkColumnType.PlanID:
                    frmQS.PlanId = (long)value;
                    break;

                case QueryStoreLinkColumnType.ObjectName:
                    newContext.ObjectName = (string)value;
                    newContext.Type = SQLTreeItem.TreeType.StoredProcedure;
                    break;

                case QueryStoreLinkColumnType.QueryHash:
                    frmQS.QueryHash = hash;
                    break;

                case QueryStoreLinkColumnType.PlanHash:
                    frmQS.PlanHash = hash;
                    break;
            }
            frmQS.Context = newContext;
            frmQS.ShowSingleInstance();
        }

        /// <summary>
        /// Query Store returns hashes as binary, but a report that filters or drills down on a hash carries it
        /// as a 0x string so it can be passed as a parameter, so both are accepted.
        /// </summary>
        private static byte[] GetHash(object value) => value switch
        {
            byte[] bytes => bytes,
            string hex => hex.HexStringToByteArray(),
            _ => null
        };
    }
}