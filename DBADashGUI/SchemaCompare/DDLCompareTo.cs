using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class DDLCompareTo : Form, IThemedControl
    {
        private readonly DiffControl diffControl = new();
        private bool _initializing;

        public List<int> SelectedTags;

        public int DatabaseID_A;
        public string Instance_A;
        public long ObjectID_A;
        public string ObjectType_A = "P";
        public DateTime SnapshotDate_A;

        public DDLCompareTo()
        {
            InitializeComponent();
        }

        private async void DDLCompareTo_Load(object sender, EventArgs e)
        {
            this.ApplyTheme();
            _initializing = true;
            try
            {
                pnlCompare.Controls.Add(diffControl);
                diffControl.Dock = DockStyle.Fill;
                var instances = CommonData.GetInstancesWithDDLSnapshot(SelectedTags);
                var objTypes = CommonData.GetObjectTypes();

                cboInstanceA.DataSource = new BindingSource(instances, null);

                cboInstanceB.DataSource = new BindingSource(instances, null);

                cboObjectTypeA.DisplayMember = "Value";
                cboObjectTypeA.ValueMember = "Key";
                cboObjectTypeA.DataSource = new BindingSource(objTypes, null);
                cboObjectTypeA.SelectedValue = ObjectType_A.TrimEnd();

                cboObjectTypeB.DisplayMember = "Value";
                cboObjectTypeB.ValueMember = "Key";
                cboObjectTypeB.DataSource = new BindingSource(objTypes, null);
                cboObjectTypeB.SelectedValue = ObjectType_A.TrimEnd();

                // Side A: populate each level and select before moving to the next
                cboInstanceA.SelectedItem = Instance_A;
                GetDatabases(cboDatabaseA, cboInstanceA.Text);
                cboDatabaseA.SelectedValue = DatabaseID_A;
                await GetObjectsA_Async();
                cboObjectA.SelectedValue = ObjectID_A;
                if (cboObjectA.SelectedValue != null)
                {
                    GetSnapshots(cboDate_A, (long)cboObjectA.SelectedValue);
                }
                cboDate_A.SelectedValue = SnapshotDate_A;

                // Side B: populate each level and match from side A
                cboInstanceB.SelectedItem = Instance_A;
                GetDatabases(cboDatabaseB, cboInstanceB.Text);
                cboDatabaseB.SelectedIndex = cboDatabaseB.FindStringExact(cboDatabaseA.Text);
                await GetObjectsB_Async();
                cboObjectB.SelectedIndex = cboObjectB.FindStringExact(cboObjectA.Text);
                if (cboObjectB.SelectedValue != null)
                {
                    GetSnapshots(cboDate_B, (long)cboObjectB.SelectedValue);
                }
            }
            catch (Exception ex)
            {
                Common.ShowExceptionDialog(ex);
            }
            finally
            {
                _initializing = false;
            }
        }

        private async Task GetObjectsB_Async()
        {
            if (cboDatabaseB.SelectedValue != null && cboObjectTypeB.SelectedValue != null)
            {
                await GetObjectsAsync(cboObjectB, (int)cboDatabaseB.SelectedValue, (string)cboObjectTypeB.SelectedValue);
            }
        }

        private async Task GetObjectsA_Async()
        {
            if (cboDatabaseA.SelectedValue != null && cboObjectTypeA.SelectedValue != null)
            {
                await GetObjectsAsync(cboObjectA, (int)cboDatabaseA.SelectedValue, (string)cboObjectTypeA.SelectedValue);
            }
        }

        private static int DropDownWidth(ComboBox myCombo)
        {
            var maxWidth = 0;
            foreach (var obj in myCombo.Items)
            {
                var temp = TextRenderer.MeasureText(myCombo.GetItemText(obj), myCombo.Font).Width;
                if (temp > maxWidth)
                {
                    maxWidth = temp;
                }
            }
            maxWidth += 20;
            return maxWidth < myCombo.Width ? myCombo.Width : maxWidth;
        }

        private static async Task GetObjectsAsync(ComboBox cbo, int DatabaseID, string type)
        {
            var dt = await CommonData.GetDBObjectsAsync(DatabaseID, type);
            cbo.DataSource = new BindingSource(dt, null);
            cbo.DisplayMember = "FullName";
            cbo.ValueMember = "ObjectID";
            cbo.DropDownWidth = DropDownWidth(cbo);
        }

        private static void GetDatabases(ComboBox cbo, string instanceGroupName)
        {
            var databases = CommonData.GetDatabasesWithDDLSnapshot(instanceGroupName);
            cbo.DataSource = databases;
            cbo.ValueMember = "DatabaseID";
            cbo.DisplayMember = "DatabaseName";
        }

        private void CboInstanceA_SelectedIndexedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            GetDatabases(cboDatabaseA, cboInstanceA.Text);
        }

        private void CboInstanceB_SelectedIndexedChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            GetDatabases(cboDatabaseB, cboInstanceB.Text);
            cboDatabaseB.SelectedIndex = cboDatabaseB.FindStringExact(cboDatabaseA.Text);
        }

        private async void CboDatabaseB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            await GetObjectsB_Async();
            cboObjectB.SelectedIndex = cboObjectB.FindStringExact(cboObjectA.Text);
        }

        private async void CboDatabaseA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            await GetObjectsA_Async();
        }

        private async void CboObjectTypeA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            await GetObjectsA_Async();
        }

        private async void CboObjectTypeB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            await GetObjectsB_Async();
        }

        private void CboObjectA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            if (cboObjectA.SelectedValue != null)
            {
                var ObjectID = (long)cboObjectA.SelectedValue;
                GetSnapshots(cboDate_A, ObjectID);
            }
        }

        private static void GetSnapshots(ComboBox cbo, long ObjectID)
        {
            var dt = CommonData.GetDDLHistoryForObject(ObjectID, 1, 200);
            cbo.DataSource = new BindingSource(dt, null);
            cbo.DisplayMember = "SnapshotValidFrom";
            cbo.ValueMember = "SnapshotValidFrom";
        }

        private void CboObjectB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            if (cboObjectB.SelectedValue != null)
            {
                var ObjectID = (long)cboObjectB.SelectedValue;
                GetSnapshots(cboDate_B, ObjectID);
            }
        }

        private void CboDate_A_SelectedIndexChanged(object sender, EventArgs e)
        {
            var row = (DataRowView)cboDate_A.SelectedItem;
            if (row != null)
            {
                if (row["DDLID"] == DBNull.Value)
                {
                    diffControl.OldText = "";
                }
                else
                {
                    var ddlID = (long)row["DDLID"];
                    diffControl.OldText = Common.DDL(ddlID);
                }
            }
        }

        private void CboDate_B_SelectedIndexChanged(object sender, EventArgs e)
        {
            var row = (DataRowView)cboDate_B.SelectedItem;
            if (row["DDLID"] == DBNull.Value)
            {
                diffControl.NewText = "";
            }
            else
            {
                var ddlID = (long)row["DDLID"];
                diffControl.NewText = Common.DDL(ddlID);
            }
        }

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            foreach (Control control in Controls)
            {
                control.ApplyTheme(theme);
            }
            diffControl.ApplyTheme(theme);
        }
    }
}