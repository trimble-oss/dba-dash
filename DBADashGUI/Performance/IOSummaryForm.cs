using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class IOSummaryForm : Form
    {
        public IOSummaryForm()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID
        {
            get => ioSummary1.InstanceID; set => ioSummary1.InstanceID = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? DatabaseID
        {
            get => ioSummary1.DatabaseID; set => ioSummary1.DatabaseID = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime FromDate
        {
            get => ioSummary1.FromDate; set => ioSummary1.FromDate = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime ToDate
        {
            get => ioSummary1.ToDate; set => ioSummary1.ToDate = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IOSummary.IOSummaryGroupByOptions GroupBy
        {
            get => ioSummary1.GroupBy; set => ioSummary1.GroupBy = value;
        }

        private void IOSummaryForm_Load(object sender, EventArgs e)
        {
            ioSummary1.RefreshData();
        }
    }
}