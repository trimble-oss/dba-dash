using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;
using OpenTK.Graphics;

namespace DBADashGUI.Performance
{
    public partial class QueryStoreViewer : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte[] QueryHash { get => queryStoreTopQueries1.QueryHash; set => queryStoreTopQueries1.QueryHash = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte[] PlanHash { get => queryStoreTopQueries1.PlanHash; set => queryStoreTopQueries1.PlanHash = value; }

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
                this.Text += " - " + Context.ObjectName;
            }
            queryStoreTopQueries1.SetContext(Context);
            if (PlanHash != null || QueryHash != null || !string.IsNullOrEmpty(Context.ObjectName))
            {
                queryStoreTopQueries1.RefreshData();
            }
        }
    }
}