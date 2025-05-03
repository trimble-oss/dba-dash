using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK.Graphics;

namespace DBADashGUI.Performance
{
    public partial class QueryStoreViewer : Form
    {
        public byte[] QueryHash { get => queryStoreTopQueries1.QueryHash; set => queryStoreTopQueries1.QueryHash = value; }
        public byte[] PlanHash { get => queryStoreTopQueries1.PlanHash; set => queryStoreTopQueries1.PlanHash = value; }

        public DBADashContext Context { get; set; }

        public QueryStoreViewer()
        {
            InitializeComponent();
        }

        private void QueryStoreViewer_Load(object sender, EventArgs e)
        {
            queryStoreTopQueries1.SetContext(Context);
            if (PlanHash != null || QueryHash != null)
            {
                queryStoreTopQueries1.RefreshData();
            }
        }
    }
}