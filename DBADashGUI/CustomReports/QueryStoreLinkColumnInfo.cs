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
            var frmQS = new QueryStoreViewer();

            var newContext = context.DeepCopy();
            if (!string.IsNullOrEmpty(InstanceIdColumn))
            {
                var instanceId = row.Cells[InstanceIdColumn].Value;
                newContext.InstanceID = (int)instanceId;
            }
            if (!string.IsNullOrEmpty(DatabaseNameColumn))
            {
                var databaseName = row.Cells[DatabaseNameColumn].Value;
                newContext.DatabaseName = (string)databaseName;
            }

            switch (TargetColumnLinkType)
            {
                case QueryStoreLinkColumnType.QueryID:
                    long queryId = (long)row.Cells[TargetColumn].Value;
                    frmQS.QueryId = queryId;
                    break;

                case QueryStoreLinkColumnType.PlanID:
                    long planId = (long)row.Cells[TargetColumn].Value;
                    frmQS.PlanId = planId;
                    break;

                case QueryStoreLinkColumnType.ObjectName:
                    string objectName = (string)row.Cells[TargetColumn].Value;
                    newContext.ObjectName = objectName;
                    newContext.Type = SQLTreeItem.TreeType.StoredProcedure;
                    break;

                case QueryStoreLinkColumnType.QueryHash:
                    byte[] objectHash = (byte[])row.Cells[TargetColumn].Value;
                    frmQS.QueryHash = objectHash;
                    break;

                case QueryStoreLinkColumnType.PlanHash:
                    byte[] planHash = (byte[])row.Cells[TargetColumn].Value;
                    frmQS.PlanHash = planHash;
                    break;
            }
            frmQS.Context = newContext;
            frmQS.ShowSingleInstance();
        }
    }
}