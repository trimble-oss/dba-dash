using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.Performance
{
    public partial class PerformanceCounterSummary : UserControl
    {
        public PerformanceCounterSummary()
        {
            InitializeComponent();
        }

        public Int32 InstanceID { get; set; }
   
        public void RefreshData()
        {
            RefreshSummary();
            RefreshChart();
        }

        private void RefreshSummary()
        {
            performanceCounterSummaryGrid1.SearchText = txtSearch.Text;
            performanceCounterSummaryGrid1.InstanceID = InstanceID;
            performanceCounterSummaryGrid1.RefreshData();
        }


        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshSummary();
            RefreshChart();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(performanceCounterSummaryGrid1);
        }

        private void RefreshChart()
        {
            if (!splitContainer1.Panel1Collapsed)
            {
                performanceCounters1.FromDate = DateRange.FromUTC;
                performanceCounters1.ToDate = DateRange.ToUTC;
                performanceCounters1.InstanceID = InstanceID;
                performanceCounters1.RefreshData();
            }
        }


        private void PerformanceCounterSummary_Load(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = true;
            performanceCounterSummaryGrid1.CounterSelected += PerformanceCounterSummaryGrid1_CounterSelected;
            performanceCounterSummaryGrid1.TextSelected += PerformanceCounterSummaryGrid1_TextSelected;
        }

        private void PerformanceCounterSummaryGrid1_TextSelected(object sender, PerformanceCounterSummaryGrid.TextSelectedEventArgs e)
        {
            txtSearch.Text = e.Text;
            RefreshSummary(); 
        }

        private void PerformanceCounterSummaryGrid1_CounterSelected(object sender, PerformanceCounterSummaryGrid.CounterSelectedEventArgs e)
        {
            splitContainer1.Panel1Collapsed = false;
            performanceCounters1.CounterID = e.CounterID;
            performanceCounters1.CounterName = e.CounterName;
            RefreshChart();
        }

        private void TsClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            RefreshSummary();
        }

        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                RefreshSummary();
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(performanceCounterSummaryGrid1);
        }

    }
}
