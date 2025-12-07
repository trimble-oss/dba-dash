using DBADash;
using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class QueryStoreViewer : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte[] QueryHash { get => queryStoreTopQueries1.QueryHash; set => queryStoreTopQueries1.QueryHash = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte[] PlanHash { get => queryStoreTopQueries1.PlanHash; set => queryStoreTopQueries1.PlanHash = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long? QueryId { get => queryStoreTopQueries1.QueryId; set => queryStoreTopQueries1.QueryId = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long? PlanId { get => queryStoreTopQueries1.PlanId; set => queryStoreTopQueries1.PlanId = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DBADashContext Context { get; set; }

        public QueryStoreViewer()
        {
            InitializeComponent();
        }

        private void QueryStoreViewer_Load(object sender, EventArgs e)
        {
            this.ApplyTheme();
            if (!string.IsNullOrEmpty(Context.ObjectName))
            {
                var text = Text + " - " + Context.InstanceName;
                if (!string.IsNullOrEmpty(Context.DatabaseName))
                {
                    text += $" - {Context.DatabaseName}";
                }
                if (QueryHash != null)
                {
                    text += $" - Query {QueryHash.ToHexString(true)}";
                }
                else if (PlanHash != null)
                {
                    text += $" - Plan {PlanHash.ToHexString(true)}";
                }
                else if (QueryId.HasValue)
                {
                    text += $" - Query {QueryId}";
                }
                else if (PlanId.HasValue)
                {
                    text += $" - Plan {PlanId}";
                }
                else if (Context.Type.IsQueryStoreObjectType())
                {
                    text += $" - {Context.ObjectName}";
                }
                Text = text;
            }
            queryStoreTopQueries1.SetContext(Context);
            if (PlanHash != null || QueryHash != null || !string.IsNullOrEmpty(Context.ObjectName))
            {
                queryStoreTopQueries1.RefreshData();
            }
        }
    }
}