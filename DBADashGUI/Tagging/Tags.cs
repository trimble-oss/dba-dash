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
using System.Data.SqlClient;
namespace DBADashGUI.Tagging
{
    public partial class Tags : UserControl
    {
        public Tags()
        {
            InitializeComponent();
        }

        public event EventHandler TagsChanged;
        private bool isTagPopulation=false;
        private List<DBADashTag> _allTags;
        public List<DBADashTag> AllTags { 
            get {
                return _allTags;
            } 
            set {
                _allTags = value == null ? new List<DBADashTag>() : value;
                cboTagName.Items.Clear();
                cboTagName.Items.AddRange(
                    _allTags.Where(t=>!t.TagName.StartsWith("{"))
                        .Select(t => t.TagName)
                        .Distinct()
                        .ToArray<object>()
                        );
            } 
        }
        public List<int> InstanceIDs { get; set; }
        public string InstanceName { get; set; }

        private void bttnAdd_Click(object sender, EventArgs e)
        {
            if (cboTagName.Text.StartsWith("{"))
            {
                MessageBox.Show("Invalid TagName.  TagNames starting with { are system reserved.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            InstanceTag newTag = new InstanceTag() { Instance = InstanceName, TagName = cboTagName.Text, TagValue = cboTagValue.Text };
            newTag.Save(Common.ConnectionString);
            RefreshData();
            TagsChanged.Invoke(this, null);
        }

        private void cboTagName_SelectedValueChanged(object sender, EventArgs e)
        {
            cboTagValue.Items.Clear();

            AllTags.Where(t => t.TagName == cboTagName.Text)
                .Select(t => cboTagValue.Items.Add(t.TagValue));
        }

        public void RefreshData()
        {
            if (string.IsNullOrEmpty(InstanceName))
            {
                splitEditReport.Panel2Collapsed = false;
                splitEditReport.Panel1Collapsed = true;
                refreshReport();
            }
            else
            {
                splitEditReport.Panel2Collapsed = true;
                splitEditReport.Panel1Collapsed = false;   
                refreshEdit();
            }

        }

        private void refreshEdit()
        {
            isTagPopulation = true;
            chkTags.Items.Clear();
            dgv.Rows.Clear();
            var tags = InstanceTag.GetInstanceTags(Common.ConnectionString, InstanceName);

            foreach (var t in tags)
            {
                if (!t.TagName.StartsWith("{"))
                {
                    chkTags.Items.Add(t, t.IsTagged);
                }
                else if (t.IsTagged)
                {
                    dgv.Rows.Add(new object[] { t.TagName.Replace("{", "").Replace("}", ""), t.TagValue });
                }
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            isTagPopulation = false;
        }

        private void refreshReport()
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.TagReport_Get", cn) { CommandType = CommandType.StoredProcedure })
            using(var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                da.Fill(dt);
                dgvReport.DataSource = dt;
                dgvReport.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void chkTags_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!isTagPopulation)
            {
                var InstanceTag = (InstanceTag)chkTags.Items[e.Index];
                if (e.NewValue == CheckState.Checked)
                {
                    InstanceTag.Save(Common.ConnectionString);
                }
                else
                {
                    InstanceTag.Delete(Common.ConnectionString);
                }
            }
        }

        private void dgvReport_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0 && e.ColumnIndex== colInstance.Index )
            {
                InstanceName = (string)dgvReport[e.ColumnIndex, e.RowIndex].Value;
                RefreshData();
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvReport);
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvReport);
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            InstanceName = string.Empty;
            RefreshData();
        }
    }



}
