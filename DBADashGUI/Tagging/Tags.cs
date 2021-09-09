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
                else if(t.IsTagged)
                {
                    dgv.Rows.Add(new object[] { t.TagName.Replace("{","").Replace("}",""), t.TagValue });
                }
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            isTagPopulation = false;
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
    }



}
