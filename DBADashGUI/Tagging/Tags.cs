using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace DBADashGUI.Tagging
{
    public partial class Tags : UserControl
    {
        public Tags()
        {
            InitializeComponent();
        }

        public event EventHandler TagsChanged;
        private List<DBADashTag> _allTags;
        public List<DBADashTag> AllTags { 
            get {
                return _allTags;
            } 
            set {
                _allTags = value ?? new List<DBADashTag>();
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
            newTag.Save();
            RefreshData();
            TagsChanged.Invoke(this, null);
        }

        private void cboTagName_SelectedValueChanged(object sender, EventArgs e)
        {
            cboTagValue.Items.Clear();

            cboTagValue.Items.AddRange(AllTags.Where(t => t.TagName == cboTagName.Text)
                                    .Select(t => (t.TagValue)).ToArray());
        }

        public void RefreshData()
        {
            lblInstance.Text = InstanceName;
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
            dgv.Rows.Clear();
            dgvTags.Rows.Clear();
            var tags = InstanceTag.GetInstanceTags(Common.ConnectionString, InstanceName);

            foreach (var t in tags)
            {
                if (!t.TagName.StartsWith("{"))
                {
                    dgvTags.Rows.Add(new object[] {t.TagID,t.IsTagged, t.TagName, t.TagValue });
                }
                else if (t.IsTagged)
                {
                    dgv.Rows.Add(new object[] { t.TagName.Replace("{", "").Replace("}", ""), t.TagValue });
                }
            }
            dgvTags.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
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
                dgvReport.Columns.Clear();
                dgvReport.Columns.Add(new DataGridViewLinkColumn() {  HeaderText = "Instance", DataPropertyName = "Instance", SortMode = DataGridViewColumnSortMode.Automatic });
                dgvReport.DataSource = dt;
                dgvReport.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }


        private void dgvReport_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0 && dgvReport.Columns[e.ColumnIndex].DataPropertyName=="Instance" )
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

        private void dgvTags_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == colCheck.Index && e.RowIndex >= 0)
            {
                dgvTags.EndEdit();
            }
        }

        private void dgvTags_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(e.ColumnIndex == colCheck.Index && e.RowIndex >= 0)
            {
                dgvTags.EndEdit();
            }
        }

        private void dgvTags_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == colCheck.Index && e.RowIndex >= 0)
            {
                int tagid = (int)dgvTags.Rows[e.RowIndex].Cells[colTagID.Index].Value;
                string tagName=(string)dgvTags.Rows[e.RowIndex].Cells[colTagName1.Index].Value;
                string tagValue = (string)dgvTags.Rows[e.RowIndex].Cells[ColTagValue1.Index].Value;
                bool isTagged = (bool)dgvTags.Rows[e.RowIndex].Cells[colCheck.Index].Value;
                var tag = new InstanceTag() { Instance = InstanceName, TagID =tagid , TagName =tagName , TagValue = tagValue, IsTagged= isTagged};
                tag.Save();
           }
        }
    }



}
