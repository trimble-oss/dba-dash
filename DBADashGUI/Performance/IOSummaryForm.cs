﻿using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Performance
{
    public partial class IOSummaryForm : Form
    {
        public IOSummaryForm()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public int InstanceID
        {
            get => ioSummary1.InstanceID; set => ioSummary1.InstanceID = value;
        }

        public int? DatabaseID
        {
            get => ioSummary1.DatabaseID; set => ioSummary1.DatabaseID = value;
        }

        public DateTime FromDate
        {
            get => ioSummary1.FromDate; set => ioSummary1.FromDate = value;
        }

        public DateTime ToDate
        {
            get => ioSummary1.ToDate; set => ioSummary1.ToDate = value;
        }

        public IOSummary.IOSummaryGroupByOptions GroupBy
        {
            get => ioSummary1.GroupBy;set=>ioSummary1.GroupBy= value;
        }

        private void IOSummaryForm_Load(object sender, EventArgs e)
        {
            ioSummary1.RefreshData();
        }
    }
}