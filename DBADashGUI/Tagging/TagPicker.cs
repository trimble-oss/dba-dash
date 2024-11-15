using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Tagging
{
    public partial class TagPicker : Form
    {
        public TagPicker()
        {
            InitializeComponent();
        }

        public DBADashTag SelectedTag { get; set; }

        private void TagPicker_Load(object sender, EventArgs e)
        {
            var tags = DBADashTag.GetTags(Common.ConnectionString);
            cboName.DataSource = tags.Select(t => t.TagName).Distinct().ToList();
            if(SelectedTag==null || SelectedTag.TagID == -1)
            {
                chkAll.Checked = true;
                return;
            }
            cboName.SelectedItem = tags.Where(t => t.TagID == SelectedTag.TagID).Select(t => t.TagName).FirstOrDefault();
            cboValue.SelectedItem = tags.Where(t => t.TagID == SelectedTag.TagID).Select(t => t.TagValue).FirstOrDefault();
        }

        private void Selected_Tag(object sender, EventArgs e)
        {
            cboValue.DataSource = DBADashTag.GetTags(Common.ConnectionString).Where(t => t.TagName == cboName.Text).Select(t => t.TagValue).Distinct().ToList();
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            SelectedTag = chkAll.Checked ? new DBADashTag() { TagID = -1, TagName = string.Empty, TagValue = string.Empty } : 
                DBADashTag.GetTags(Common.ConnectionString).FirstOrDefault(t => t.TagName == cboName.Text && t.TagValue == cboValue.Text);
            this.DialogResult = DialogResult.OK;
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void All_CheckChanged(object sender, EventArgs e)
        {
            cboValue.Enabled=!chkAll.Checked;
            cboName.Enabled = !chkAll.Checked;
        }
    }
}
