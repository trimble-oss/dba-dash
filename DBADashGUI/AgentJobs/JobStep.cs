using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class JobStep : UserControl
    {
        public JobStep()
        {
            InitializeComponent();
        }

        public int InstanceID;
        public Guid JobID;
        public int StepID;

        private DataTable GetJobStep()
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.JobStep_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("JobID", JobID);
                cmd.Parameters.AddWithValue("StepID", StepID);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public void RefreshData()
        {
            var dt = GetJobStep();
            if (dt.Rows.Count == 1)
            {
                var r = dt.Rows[0];
                string subsystem = (string)r["subsystem"];
                txtJobStep.Text = (string)r["command"];
                lblJobStep.Text = (string)r["name"] + " | " + r["step_name"];
                if (subsystem == "TransactSql")
                {
                    txtJobStep.SetHighlighting("SQL");
                }
                else if (subsystem == "PowerShell")
                {
                    txtJobStep.SetHighlighting("PowerShell");
                }
                else
                {
                    txtJobStep.SetHighlighting("");
                }
            }
        }

        private void JobStep_Load(object sender, EventArgs e)
        {
            HighlightingManager.Manager.AddSyntaxModeFileProvider(new AppSyntaxModeProvider());
        }
    }
}
