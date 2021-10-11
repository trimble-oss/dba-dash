using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADash;
using DBADashService;
using Humanizer;
using Newtonsoft.Json;
using Quartz;

namespace DBADashServiceConfig
{
    public partial class ScheduleConfig : Form
    {
        public ScheduleConfig()
        {
            InitializeComponent();
        }

        CollectionSchedules userSchedule;
        public CollectionSchedules BaseSchedule = CollectionSchedules.DefaultSchedules;

        public CollectionSchedules ConfiguredSchedule
        {
            get
            {
                var schedule = new CollectionSchedules();
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (!(bool)row.Cells["Default"].Value) {
                        schedule.Add((CollectionType)Enum.Parse(typeof(CollectionType), (string)row.Cells["CollectionType"].Value), new CollectionSchedule() { Schedule = (string)row.Cells["Schedule"].Value, RunOnServiceStart = (bool)row.Cells["RunOnStart"].Value });
                    }
                }
                return schedule;
            }
            set
            {
                userSchedule = value;
            }
        }

        private void ScheduleConfig_Load(object sender, EventArgs e)
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CollectionType", HeaderText = "Collection Type", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() {Name="Schedule", HeaderText = "Schedule" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() {Name="ScheduleDescription", HeaderText = "Schedule Description", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn() {Name="RunOnStart", HeaderText = "Run on service start" });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn() {Name="Default", HeaderText = "Default" });

            if (userSchedule != null)
            {
                foreach (var s in userSchedule)
                {
                    int idx = dgv.Rows.Add(new object[] { Enum.GetName(typeof(CollectionType), s.Key), s.Value.Schedule, getScheduleDescription(s.Value.Schedule), s.Value.RunOnServiceStart, false });
                    formatRow(idx);
                }
            }
            foreach (var s in BaseSchedule)
            {
                if (userSchedule==null || !userSchedule.ContainsKey(s.Key))
                {
                    int idx = dgv.Rows.Add(new object[] { Enum.GetName(typeof(CollectionType), s.Key), s.Value.Schedule, getScheduleDescription(s.Value.Schedule), s.Value.RunOnServiceStart, true });
                    formatRow(idx);
                }
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private string getScheduleDescription(string schedule)
        {
            if (string.IsNullOrEmpty(schedule))
            {
                return "Disabled";
            }
            if (int.TryParse(schedule,out int seconds))
            {

                return TimeSpan.FromSeconds(seconds).Humanize(5);
            }
            else
            {
                if (CronExpression.IsValidExpression(schedule)) // Check expression is valid for Quartz
                {
                    return CronExpressionDescriptor.ExpressionDescriptor.GetDescription(schedule);
                }
                else
                {
                    throw new Exception("Invalid cron expression");
                }
            }
        }

        private void dgv_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            string schedule = (string)dgv[1,e.RowIndex].Value;
            
            try
            {                    
                dgv[2, e.RowIndex].Value =getScheduleDescription(schedule);              
            }
            catch
            {
                MessageBox.Show("Invalid cron expression","Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        private void dgv_CellValidated(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e) { 
       
            for (int i= 0; i < e.RowCount - 1; i++){
                dgv.Rows[e.RowIndex + i].Cells["Schedule"].ReadOnly = (bool)dgv.Rows[e.RowIndex + i].Cells["Default"].Value;
                dgv.Rows[e.RowIndex + i].Cells["RunOnStart"].ReadOnly = (bool)dgv.Rows[e.RowIndex + i].Cells["Default"].Value;
            }
        }

        private void dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if(e.ColumnIndex == dgv.Columns["Default"].Index)
            {
                formatRow(e.RowIndex);
            }
  
        }

        private void formatRow(int idx)
        {
            var row = dgv.Rows[idx];
            bool isDefault = (bool)row.Cells["Default"].Value;
            row.Cells["Schedule"].ReadOnly = isDefault;
            row.Cells["RunOnStart"].ReadOnly = isDefault;
            row.Cells["Schedule"].Style.BackColor = isDefault ? Color.LightGray : Color.White;
            row.Cells["RunOnStart"].Style.BackColor = isDefault ? Color.LightGray : Color.White;
            row.Cells["ScheduleDescription"].Style.BackColor = isDefault ? Color.LightGray : Color.AliceBlue;
            row.Cells["CollectionType"].Style.BackColor = isDefault ? Color.LightGray : Color.AliceBlue;
            if (isDefault)
            {
                CollectionType collectType = (CollectionType)Enum.Parse(typeof(CollectionType), (string)row.Cells["CollectionType"].Value);
                row.Cells["Schedule"].Value = BaseSchedule[collectType].Schedule;
                row.Cells["RunOnStart"].Value = BaseSchedule[collectType].RunOnServiceStart;
                row.Cells["ScheduleDescription"].Value = getScheduleDescription(BaseSchedule[collectType].Schedule);
            }
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["Default"].Index)
            {
                dgv.EndEdit();
                formatRow(e.RowIndex);
            }
        }

         private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void lnkCron_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.cronmaker.com/");
        }
    }
}
