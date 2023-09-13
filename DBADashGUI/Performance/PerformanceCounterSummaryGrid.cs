using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DBADashGUI.Theme;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.Performance
{
    public class PerformanceCounterSummaryGrid : DataGridView
    {
        public Int32 InstanceID { get; set; }
        public string SearchText { get; set; }
        public List<int> Counters { get; set; }

        public event EventHandler<CounterSelectedEventArgs> CounterSelected;

        public event EventHandler<TextSelectedEventArgs> TextSelected;

        public bool ObjectLink = true;
        public bool CounterLink = true;
        public bool InstanceLink = true;

        public class CounterSelectedEventArgs : EventArgs
        {
            public int CounterID { get; set; }
            public string CounterName { get; set; }
        }

        public class TextSelectedEventArgs : EventArgs
        {
            public string Text { get; set; }
        }

        public void RefreshData()
        {
            RefreshSummary();
        }

        public PerformanceCounterSummaryGrid()
        {
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            ReadOnly = true;
            BackgroundColor = Color.White;
            AutoGenerateColumns = false;
            RowHeadersVisible = false;
            RowsAdded += PerformanceCounterSummaryGrid_RowsAdded;
            CellContentClick += PerformanceCounterSummaryGrid_CellContentClick;
        }

        private void RefreshSummary()
        {
            AddCols();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.PerformanceCounterSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                if (!String.IsNullOrEmpty(SearchText))
                {
                    cmd.Parameters.AddWithValue("Search", $"%{SearchText}%");
                }
                if (Counters != null && Counters.Count > 0)
                {
                    cmd.Parameters.AddWithValue("Counters", string.Join(",", Counters));
                }
                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }
                cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
                DataTable dt = new();
                da.Fill(dt);
                AutoGenerateColumns = false;
                DataSource = dt;

                AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                this.ApplyTheme();
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshSummary();
        }

        private void PerformanceCounterSummaryGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)Rows[e.RowIndex].DataBoundItem;
                var objectName = (string)row["object_name"];
                var counterName = (string)row["counter_name"];
                var instanceName = (string)row["instance_name"];
                var colName = Columns[e.ColumnIndex].Name;
                if (colName == "lnkView")
                {
                    string counter = $"{objectName}\\{counterName}\\{instanceName}".Trim('\\');
                    CounterSelected?.Invoke(this, new CounterSelectedEventArgs() { CounterID = (int)row["CounterID"], CounterName = counter });
                }
                else if (colName == "lnkCounter")
                {
                    TextSelected?.Invoke(this, new TextSelectedEventArgs() { Text = counterName });
                }
                else if (colName == "lnkInstance")
                {
                    TextSelected?.Invoke(this, new TextSelectedEventArgs() { Text = instanceName });
                }
                else if (colName == "lnkObject")
                {
                    TextSelected?.Invoke(this, new TextSelectedEventArgs() { Text = objectName });
                }
                else if (colName == "lnkThresholds")
                {
                    using (var frm = new PerformanceCounterThreshold())
                    {
                        frm.CounterName = counterName;
                        frm.CounterInstance = instanceName;
                        frm.ObjectName = objectName;
                        frm.InstanceID = InstanceID;
                        frm.ShowDialog(this);
                        if (frm.DialogResult == DialogResult.OK)
                        {
                            RefreshSummary();
                        }
                    }
                }
            }
        }

        private void PerformanceCounterSummaryGrid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)Rows[idx].DataBoundItem;
                if (row["CriticalFrom"] == DBNull.Value && row["CriticalTo"] == DBNull.Value && row["WarningFrom"] == DBNull.Value && row["WarningTo"] == DBNull.Value && row["GoodFrom"] == DBNull.Value && row["GoodTo"] == DBNull.Value)
                {
                    Rows[idx].Cells["colMinValue"].SetStatusColor(DBADashStatusEnum.NA);
                    Rows[idx].Cells["colMaxValue"].SetStatusColor(DBADashStatusEnum.NA);
                    Rows[idx].Cells["colAVGValue"].SetStatusColor(DBADashStatusEnum.NA);
                    Rows[idx].Cells["colCurrentValue"].SetStatusColor(DBADashStatusEnum.NA);
                }
                else
                {
                    Rows[idx].Cells["colMinValue"].SetStatusColor((DBADashStatusEnum)row["MinValueStatus"]);
                    Rows[idx].Cells["colMaxValue"].SetStatusColor((DBADashStatusEnum)row["MaxValueStatus"]);
                    Rows[idx].Cells["colAVGValue"].SetStatusColor((DBADashStatusEnum)row["AvgValueStatus"]);
                    Rows[idx].Cells["colCurrentValue"].SetStatusColor((DBADashStatusEnum)row["CurrentValueStatus"]);
                }
            }
        }

        private void AddCols()
        {
            if (Columns.Count == 0)
            {
                Columns.AddRange(
                    new DataGridViewLinkColumn() { Name = "lnkObject", HeaderText = "Object", DataPropertyName = "object_name", LinkColor = DashColors.LinkColor, Visible = ObjectLink, SortMode = DataGridViewColumnSortMode.Automatic },
                    new DataGridViewLinkColumn() { Name = "lnkCounter", HeaderText = "Counter", DataPropertyName = "counter_name", LinkColor = DashColors.LinkColor, Visible = CounterLink, SortMode = DataGridViewColumnSortMode.Automatic },
                    new DataGridViewLinkColumn() { Name = "lnkInstance", HeaderText = "Instance", DataPropertyName = "instance_name", LinkColor = DashColors.LinkColor, Visible = InstanceLink, SortMode = DataGridViewColumnSortMode.Automatic },
                    new DataGridViewTextBoxColumn() { Name = "colObject", HeaderText = "Object", DataPropertyName = "object_name", Visible = !ObjectLink },
                    new DataGridViewTextBoxColumn() { Name = "colCounter", HeaderText = "Counter", DataPropertyName = "counter_name", Visible = !CounterLink },
                    new DataGridViewTextBoxColumn() { Name = "colInstance", HeaderText = "Instance", DataPropertyName = "instance_name", Visible = !InstanceLink },
                    new DataGridViewTextBoxColumn() { Name = "colMaxValue", HeaderText = "Max Value", DataPropertyName = "MaxValue", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                    new DataGridViewTextBoxColumn() { Name = "colMinValue", HeaderText = "Min Value", DataPropertyName = "MinValue", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                    new DataGridViewTextBoxColumn() { Name = "colAvgValue", HeaderText = "Avg Value", DataPropertyName = "AvgValue", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                    new DataGridViewTextBoxColumn() { Name = "colTotal", HeaderText = "Total", DataPropertyName = "TotalValue", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                    new DataGridViewTextBoxColumn() { Name = "colSampleCount", HeaderText = "Sample Count", DataPropertyName = "SampleCount", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                    new DataGridViewTextBoxColumn() { Name = "colCurrentValue", HeaderText = "Current Value", DataPropertyName = "CurrentValue", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                    new DataGridViewLinkColumn() { Name = "lnkThresholds", HeaderText = "Thresholds", Text = "Edit", LinkColor = DashColors.LinkColor, UseColumnTextForLinkValue = true },
                    new DataGridViewLinkColumn() { Name = "lnkView", HeaderText = "Chart", Text = "View", LinkColor = DashColors.LinkColor, UseColumnTextForLinkValue = true, ToolTipText = "Click to view chart." }
                );
            }
        }

        private void TsClear_Click(object sender, EventArgs e)
        {
            RefreshSummary();
        }
    }
}