﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using LiveCharts;
using static DBADashGUI.Performance.Performance;
using System.Xml.Schema;

namespace DBADashGUI.Performance
{
    public partial class Waits : UserControl
    {
        public Waits()
        {
            InitializeComponent();
        }

        public class DateModel
        {
            public System.DateTime DateTime { get; set; }
            public double Value { get; set; }
        }

        public DateTimePoint x;
        Int32 instanceID;
        DateTime lastWait = DateTime.MinValue;
        private Int32 dateGrouping;
        Int32 mins;
        string _waitType;

        public string WaitType
        {
            get
            {
                return _waitType;
            }
            set
            {
                tsFilter.Text = value == "" ? (criticalWaitsOnlyToolStripMenuItem.Checked ? "*Critical Waits*":"") : value;
                _waitType = value;
            }
        }


        public void RefreshData(Int32 instanceID)
        {
            this.instanceID = instanceID;

            if(mins!= DateRange.DurationMins)
            {
                dateGrouping = Common.DateGrouping(DateRange.DurationMins, 35);
                if (dateGrouping < 1)
                {
                    dateGrouping = 1;
                }
                tsDateGrouping.Text = Common.DateGroupString(dateGrouping);
                mins = DateRange.DurationMins;
            }
        
            RefreshData();
        }


        private DataTable GetWaitsDT()
        {

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Waits_Get", cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
                cmd.Parameters.AddWithValue("CriticalWaitsOnly", criticalWaitsOnlyToolStripMenuItem.Checked);
                if (_waitType != null && _waitType.Length > 0)
                {
                    cmd.Parameters.AddWithValue("WaitType", _waitType);
                }
                cmd.Parameters.AddWithValue("@UTCOffset", Common.UtcOffset);
                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }
                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        public void RefreshData()
        {

            waitChart.Series.Clear();
            waitChart.AxisX.Clear();
            waitChart.AxisY.Clear();
            lastWait = DateTime.MinValue;

            var dt = GetWaitsDT();

            if (dt.Rows.Count == 0)
            {
                return;
            }
            var dPoints = new Dictionary<string, ChartValues<DateTimePoint>>();
            string current = string.Empty;
            ChartValues<DateTimePoint> values = new();
            foreach (DataRow r in dt.Rows)
            {
                var waitType = (string)r["WaitType"];
                var time = (DateTime)r["Time"];
                if (time > lastWait)
                {
                    lastWait = time;
                }
                if (current != waitType)
                {
                    if (values.Count > 0) { dPoints.Add(current, values); }
                    values = new ChartValues<DateTimePoint>();
                    current = waitType;
                }
                values.Add(new DateTimePoint(((DateTime)r["Time"]).ToLocalTime(), (double)(decimal)r["WaitTimeMsPerSec"]));
            }
            if (values.Count > 0)
            {
                dPoints.Add(current, values);
                values = new ChartValues<DateTimePoint>();
            }


            CartesianMapper<DateTimePoint> dayConfig = Mappers.Xy<DateTimePoint>()
.X(dateModel => dateModel.DateTime.Ticks / TimeSpan.FromMinutes(dateGrouping == 0 ? 1 : dateGrouping).Ticks)
.Y(dateModel => dateModel.Value);


            SeriesCollection s1 = new(dayConfig);
            foreach (var x in dPoints)
            {
                s1.Add(new StackedColumnSeries
                {
                    Title = x.Key,
                    Values = x.Value
                });
            }
            waitChart.Series = s1;

            string format = "t";
            if (dateGrouping >= 1440)
            {
                format = "yyyy-MM-dd";
            }
            else if (mins >= 1440)
            {
                format = "yyyy-MM-dd HH:mm";
            }
            waitChart.AxisX.Add(new Axis
            {
                LabelFormatter = value => new DateTime((long)(value * TimeSpan.FromMinutes(dateGrouping == 0 ? 1 : dateGrouping).Ticks)).ToString(format)
            });
            waitChart.AxisY.Add(new Axis
            {
                LabelFormatter = val => val.ToString("0ms/sec")

            });
        }
    

    private void Waits_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = Common.DateGroupString(dateGrouping);
            RefreshData();
        }

        private void TsFilterWaitType_Click(object sender, EventArgs e)
        {
            string wt = _waitType;
            if (Common.ShowInputDialog(ref wt,"Wait Type (LIKE):") == DialogResult.OK)
            {
                WaitType = wt.EndsWith("%") || wt.Length==0 ? wt : wt+"%";
                RefreshData();
            }
        }

        private void StringFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string wt = _waitType;
            if (Common.ShowInputDialog(ref wt, "Wait Type (LIKE):") == DialogResult.OK)
            {
                WaitType = wt.EndsWith("%") || wt.Length == 0 ? wt : wt + "%";
                criticalWaitsOnlyToolStripMenuItem.Checked = false;
                RefreshData();
            }
        }

        private void CriticalWaitsOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WaitType = "";
            RefreshData();
        }
    }
}
