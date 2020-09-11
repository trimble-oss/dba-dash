﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using LiveCharts;
using static DBAChecksGUI.Performance.Performance;

namespace DBAChecksGUI.Performance
{
    public partial class ObjectExecution : UserControl
    {
        public ObjectExecution()
        {
            InitializeComponent();
        }

        public class DateModel
        {
            public System.DateTime DateTime { get; set; }
            public double Value { get; set; }
        }

        public string measure = "TotalDuration";
        public DateTimePoint x;
        Int32 instanceID;
        DateTime chartMaxDate = DateTime.MinValue;
        string connectionString;
        DateGroup dateGrouping;
        DateTime from;
        DateTime to;
        Int32 mins;
        private Int64 objectID;
        Int32 databaseid=0;

        public void RefreshData()
        {
            if (chartMaxDate != DateTime.MinValue && dateGrouping == DateGroup._1MIN)
            {
                this.to = DateTime.UtcNow.AddMinutes(1);
                this.from = chartMaxDate.AddMinutes(-utcOffset()).AddSeconds(1);
                refreshData(true);
            }
        }
        public void RefreshData(Int32 instanceID, DateTime from, DateTime to, string connectionString, Int64 objectID, Int32 databaseID, DateGroup dateGrouping = DateGroup.None)
        {
            this.instanceID = instanceID;
            this.connectionString = connectionString;
            this.from = from;
            this.to = to;
            this.objectID = objectID;
            mins = (Int32)to.Subtract(from).TotalMinutes;
            this.dateGrouping = dateGrouping;
            this.databaseid = databaseID;
            refreshData(false);
        }

        private Int32 utcOffset()
        {
           return  (Int32)DateTime.Now.Subtract(DateTime.UtcNow).TotalMinutes;
        }

         private void refreshData(bool update)
        {

            if (!update)
            {
                waitChart.Series.Clear();
                waitChart.AxisX.Clear();
                waitChart.AxisY.Clear();
                chartMaxDate = DateTime.MinValue;
            }
         

            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.ObjectExecutionStats_Get", cn);
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("FromDateUTC", from);
                cmd.Parameters.AddWithValue("ToDateUTC", to);
                cmd.Parameters.AddWithValue("UTCOffset", utcOffset());
                if (objectID > 0)
                {
                    cmd.Parameters.AddWithValue("ObjectID", objectID);
                }
                cmd.Parameters.AddWithValue("DateAgg", dateGrouping.ToString().Replace("_",""));
                cmd.Parameters.AddWithValue("Measure", measure);
                if (this.databaseid > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", databaseid);
                }
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if(dt.Rows.Count == 0)
                {
                    return;
                }
                var dPoints = new Dictionary<string, ChartValues<DateTimePoint>>();
                string current = string.Empty;
                ChartValues<DateTimePoint> values = new ChartValues<DateTimePoint>();               
                foreach (DataRow r in dt.Rows){
                    var waitType =(string)r["DatabaseName"] + " | " + (string)r["object_name"];
                    var time = (DateTime)r["SnapshotDate"];
                    if (time > chartMaxDate)
                    {
                        chartMaxDate = time;
                    }
                    if (current!= waitType)
                    {
                        if (values.Count > 0) { dPoints.Add(current, values); }
                        values = new ChartValues<DateTimePoint>();
                        current = waitType;
                    }
                    values.Add(new DateTimePoint(((DateTime)r["SnapshotDate"]), Convert.ToDouble(r["Measure"])));
                }
                if (values.Count > 0)
                {
                    dPoints.Add(current, values);
                    values = new ChartValues<DateTimePoint>();
                }


                Int32 fromMins = 1;
                if(dateGrouping == DateGroup.DAY)
                {
                    fromMins = 1440;
                }
                else if( dateGrouping == DateGroup.None || dateGrouping == DateGroup._1MIN)
                {
                    fromMins = 1;
                }
                else if (dateGrouping == DateGroup._10MIN)
                {
                    fromMins = 10;
                }
                else if(dateGrouping == DateGroup._60MIN)
                {
                    fromMins = 60;
                }
                else if (dateGrouping == DateGroup._120MIN)
                {
                    fromMins = 120;
                }
                else
                {
                    throw new NotImplementedException("dateGrouping");
                }

                if (update)
                {
                     List<string> existingTitles = new List<string>();
                    foreach(StackedColumnSeries s in waitChart.Series)
                    {
                        existingTitles.Add(s.Title);
                        if (dPoints.ContainsKey(s.Title))
                        {
                            values = dPoints[s.Title];
                            s.Values.AddRange(values);
                        }
                     
                        while(s.Values.Count>0 && DateTime.Now.Subtract(((DateTimePoint)s.Values[0]).DateTime).TotalMinutes > mins)
                        {
                            s.Values.RemoveAt(0);
                        }
                    }
                    foreach (var x in dPoints)
                    {
                        if (!existingTitles.Contains(x.Key))
                        {
                            waitChart.Series.Add(new StackedColumnSeries
                            {
                                Title = x.Key,
                                Values = x.Value
                            });
                        }
                    }
                }
                else
                {
                    CartesianMapper<DateTimePoint> dayConfig = Mappers.Xy<DateTimePoint>()
.X(dateModel => dateModel.DateTime.Ticks / TimeSpan.FromMinutes(fromMins).Ticks)
.Y(dateModel => dateModel.Value);


                    SeriesCollection s1 = new SeriesCollection(dayConfig);
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
                    if (fromMins >= 1440)
                    {
                        format = "yyyy-MM-dd";
                    }
                    else if (mins >= 1440)
                    {
                        format = "yyyy-MM-dd HH:mm";
                    }
                    waitChart.AxisX.Add(new Axis
                    {
                        LabelFormatter = value => new DateTime((long)(value * TimeSpan.FromMinutes(fromMins).Ticks)).ToString(format)
                    });
                   
                    waitChart.AxisY.Add(new Axis
                    {
                        LabelFormatter = val => val.ToString(measures[measure].LabelFormat)

                    });

                }

            }
            lblExecution.Text = databaseid > 0 ? "Excution Stats: Database" : "Execution Stats: Instance";
        }

        private class Measure
        {

            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string LabelFormat { get; set; }


        }

        private class Measures: Dictionary<string, Measure>
        {
            public void Add(string Name,string displayName,string labelFormat)
            {
                Add(Name, new Measure() { Name = Name, DisplayName = displayName, LabelFormat = labelFormat });
            }

        }

        private Measures measures = new Measures()
            {
                {"TotalDuration", "Total Duration","#,##0.000 sec"},
                {"AvgDuration", "Avg Duration","#,##0.000 sec"},
                {"TotalCPU", "Total CPU","#,##0.000 sec"},
                {"AvgCPU","Avg CPU", "#,##0.000  sec"},
                {"ExecutionCount", "Execution Count","N0"},
                {"ExecutionsPerMin", "Executions Per Min","#,##0.000"},
                { "TotalLogicalReads","Total Logical Reads","N0" },
                {"AvgLogicalReads","Avg Logical Reads","N0" },
                {"TotalPhysicalReads","TotalPhysicalReads" ,"N0"},
                {"AvgPhysicalReads","Avg Physical Reads" ,"N0"},
                {"TotalWrites","Total Writes" ,"N0"},
                {"AvgWrites","Avg Writes","N0" }

            };

        private void ObjectExecution_Load(object sender, EventArgs e)
        {

            foreach(var m in measures)
            {
                ToolStripMenuItem itm = new ToolStripMenuItem(m.Value.DisplayName);
                itm.Name = m.Key;
                if (m.Key == measure) { 
                    itm.Checked = true;
                    tsMeasures.Text = m.Value.DisplayName;
                }

                itm.Click += Itm_Click;
                tsMeasures.DropDownItems.Add(itm);
            }
        }

        private void Itm_Click(object sender, EventArgs e)
        {
            var tsItm = ((ToolStripMenuItem)sender);
            if(measure!= tsItm.Name)
            {
                measure = tsItm.Name;
                foreach(ToolStripMenuItem itm in tsMeasures.DropDownItems)
                {
                    itm.Checked = itm.Name == measure;
                }
                tsMeasures.Text = tsItm.Text;
            }
            RefreshData(instanceID, to.AddMinutes(-mins), to, connectionString,objectID, databaseid, dateGrouping);
        }
    }
}
