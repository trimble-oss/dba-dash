﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveCharts.Defaults;
using Microsoft.Data.SqlClient;
using LiveCharts.Wpf;
using LiveCharts;
using static DBADashGUI.Performance.Performance;

namespace DBADashGUI.Performance
{
    public partial class CPU : UserControl
    {
        public CPU()
        {
            InitializeComponent();
        }

        DateTime eventTime = DateTime.MinValue;
        Int32 mins;

        string connectionString;
        Int32 InstanceID;
        DateTime fromDate;
        DateTime toDate;
        Int32 DateGrouping;
        bool smoothLines = true;

        public Int32 PointSize;

        public bool SmoothLines
        {
            get
            {
                return smoothLines;
            }
            set
            {
                smoothLines = value;
                foreach (Series s in chartCPU.Series)
                {
                    if (s.GetType() == typeof(LineSeries))
                    {
                        ((LineSeries)s).LineSmoothness = smoothLines ? 1 : 0;
                    }
                    else if (s.GetType() == typeof(StackedAreaSeries))
                    {
                        ((StackedAreaSeries)s).LineSmoothness = smoothLines ? 1 : 0;
                    }

                }
            }
        }


        public void RefreshData(Int32 InstanceID, DateTime fromDate, DateTime toDate, string connectionString)
        {
            eventTime = DateTime.MinValue;
            mins = (Int32)toDate.Subtract(fromDate).TotalMinutes;
            this.InstanceID = InstanceID;
            if (this.fromDate != fromDate || this.toDate != toDate)
            {
                DateGrouping = Common.DateGrouping(mins, 200);
                tsDateGrouping.Text = Common.DateGroupString(DateGrouping);
            }
            this.fromDate = fromDate;
            this.toDate = toDate;
            this.connectionString = connectionString;
            refreshData(false);
        }

        public void RefreshData()
        {
            if (eventTime > DateTime.MinValue)
            {
                fromDate = eventTime.AddSeconds(-30);
                toDate = DateTime.UtcNow.AddSeconds(60);
                refreshData(true);
            }
        }

        private void refreshData(bool update = false)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.CPU_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    cmd.Parameters.AddWithValue("@DateGroupingMin", DateGrouping);
                    cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                    var rdr = cmd.ExecuteReader();

                    var sqlProcessValues = new ChartValues<DateTimePoint>();
                    var otherValues = new ChartValues<DateTimePoint>();
                    var maxValues = new ChartValues<DateTimePoint>();

                    while (rdr.Read())
                    {
                        eventTime = (DateTime)rdr["EventTime"];
                        sqlProcessValues.Add(new DateTimePoint(eventTime.ToLocalTime(), Decimal.ToDouble((decimal)rdr["SQLProcessCPU"]) / 100.0));
                        otherValues.Add(new DateTimePoint(eventTime.ToLocalTime(), Decimal.ToDouble((decimal)rdr["OtherCPU"]) / 100.0));
                        maxValues.Add(new DateTimePoint(eventTime.ToLocalTime(), Decimal.ToDouble((decimal)rdr["MaxCPU"]) / 100.0));

                    }
                    if (update && maxValues.Count == 0)
                    {
                        return;
                    }
                    if (update)
                    {
                        var cnt = chartCPU.Series[0].Values.Count;
                        if (cnt > 0)
                        {
                            while (((DateTimePoint)chartCPU.Series[0].Values[cnt - 1]).DateTime >= sqlProcessValues[0].DateTime)
                            {
                                chartCPU.Series[0].Values.RemoveAt(cnt - 1);
                                chartCPU.Series[1].Values.RemoveAt(cnt - 1);
                                chartCPU.Series[2].Values.RemoveAt(cnt - 1);
                                cnt -= 1;
                            }
                        }

                        chartCPU.Series[0].Values.AddRange(sqlProcessValues);
                        chartCPU.Series[1].Values.AddRange(otherValues);
                        chartCPU.Series[2].Values.AddRange(maxValues);
                        while (chartCPU.Series[0].Values.Count > mins)
                        {
                            if (((DateTimePoint)chartCPU.Series[0].Values[0]).DateTime > DateTime.Now.AddMinutes(-mins))
                            {
                                break;
                            }
                            else
                            {
                                if (chartCPU.Series[0].Values.Count > 0 && DateTime.Now.Subtract(((DateTimePoint)chartCPU.Series[0].Values[0]).DateTime).TotalMinutes > mins)
                                {
                                    chartCPU.Series[0].Values.RemoveAt(0);
                                    chartCPU.Series[1].Values.RemoveAt(0);
                                    chartCPU.Series[2].Values.RemoveAt(0);
                                }
                            }
                        }
                    }
                    else
                    {
                        SeriesCollection s1 = new SeriesCollection
                    {
                        new StackedAreaSeries
                        {
                            Title="SQL Process",
                            Values = sqlProcessValues,
                            LineSmoothness = SmoothLines ? 1 : 0
                        },
                        new StackedAreaSeries
                        {
                        Title = "Other",
                        Values = otherValues,
                        LineSmoothness = SmoothLines ? 1 : 0
                        },
                        new LineSeries
                        {
                        Title = "Max CPU",
                        Values = maxValues,
                        LineSmoothness = SmoothLines ? 1 : 0,
                        PointGeometrySize = PointSize,
                        }
                    };
                        chartCPU.AxisX.Clear();
                        chartCPU.AxisY.Clear();
                        string format = mins < 1440 ? "HH:mm" : "yyyy-MM-dd HH:mm";
                        chartCPU.AxisX.Add(new Axis
                        {
                            LabelFormatter = val => new System.DateTime((long)val).ToString(format)
                        });
                        chartCPU.AxisY.Add(new Axis
                        {
                            LabelFormatter = val => val.ToString("P1"),
                            MaxValue = 1,
                            MinValue = 0,

                        });
                        if (maxValues.Count > 1)
                        {
                            chartCPU.Series = s1;
                        }
                        else
                        {
                            chartCPU.Series.Clear();
                        }
                        updateVisibility();
                    }

                }
            }

        }

        private void updateVisibility()
        {
            if (chartCPU.Series.Count == 3)
            {
                ((StackedAreaSeries)chartCPU.Series[0]).Visibility = AVGToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                ((StackedAreaSeries)chartCPU.Series[1]).Visibility = AVGToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                ((LineSeries)chartCPU.Series[2]).Visibility = MAXToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
        }

        private void AVGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MAXToolStripMenuItem.Checked = (!AVGToolStripMenuItem.Checked);
            updateVisibility();
        }

        private void MAXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AVGToolStripMenuItem.Checked = (!MAXToolStripMenuItem.Checked);
            updateVisibility();
        }

        private void CPU_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);

        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = Common.DateGroupString(DateGrouping);
            refreshData();

        }
    }
}
