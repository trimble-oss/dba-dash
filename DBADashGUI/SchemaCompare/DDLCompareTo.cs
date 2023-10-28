using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class DDLCompareTo : Form, IThemedControl
    {
        private readonly DiffControl diffControl = new();

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

        private void DDLCompareTo_Load(object sender, EventArgs e)
        {
            pnlCompare.Controls.Add(diffControl);
            diffControl.Dock = DockStyle.Fill;
            var instances = CommonData.GetInstancesWithDDLSnapshot(SelectedTags);
            Dictionary<string, string> objtypes = CommonData.GetObjectTypes();

            cboInstanceA.DataSource = new BindingSource(instances, null);

            cboInstanceB.DataSource = new BindingSource(instances, null);

            cboObjectTypeA.DisplayMember = "Value";
            cboObjectTypeA.ValueMember = "Key";
            cboObjectTypeA.DataSource = new BindingSource(objtypes, null);
            cboObjectTypeA.SelectedValue = ObjectType_A.TrimEnd();

            cboObjectTypeB.DisplayMember = "Value";
            cboObjectTypeB.ValueMember = "Key";
            cboObjectTypeB.DataSource = new BindingSource(objtypes, null);
            cboObjectTypeB.SelectedValue = ObjectType_A.TrimEnd();

            cboInstanceA.SelectedItem = Instance_A;
            cboDatabaseA.SelectedValue = DatabaseID_A;
            cboObjectA.SelectedValue = ObjectID_A;
            cboDate_A.SelectedValue = SnapshotDate_A;

            cboInstanceB.SelectedItem = Instance_A;
            cboDatabaseB.SelectedIndex = cboDatabaseB.FindStringExact(cboDatabaseA.Text);
            cboObjectB.SelectedIndex = cboObjectB.FindStringExact(cboObjectA.Text);
        }

        private void GetObjectsB()
        {
            if (cboDatabaseB.SelectedValue != null && cboObjectTypeB.SelectedValue != null)
            {
                GetObjects(cboObjectB, (int)cboDatabaseB.SelectedValue, (string)cboObjectTypeB.SelectedValue);
            }
        }

        private void GetObjectsA()
        {
            if (cboDatabaseA.SelectedValue != null && cboObjectTypeA.SelectedValue != null)
            {
                GetObjects(cboObjectA, (int)cboDatabaseA.SelectedValue, (string)cboObjectTypeA.SelectedValue);
            }
        }

        private static int DropDownWidth(ComboBox myCombo)
        {
            int maxWidth = 0;
            foreach (var obj in myCombo.Items)
            {
                int temp = TextRenderer.MeasureText(myCombo.GetItemText(obj), myCombo.Font).Width;
                if (temp > maxWidth)
                {
                    maxWidth = temp;
                }
            }
            maxWidth += 20;
            if (maxWidth < myCombo.Width)
            {
                return myCombo.Width;
            }
            else
            {
                return maxWidth;
            }
        }

        private static void GetObjects(ComboBox cbo, int DatabaseID, string type)
        {
            DataTable dt = CommonData.GetDBObjects(DatabaseID, type);
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
            GetDatabases(cboDatabaseA, cboInstanceA.Text);
        }

        private void CboInstanceB_SelectedIndexedChanged(object sender, EventArgs e)
        {
            GetDatabases(cboDatabaseB, cboInstanceB.Text);
            cboDatabaseB.SelectedIndex = cboDatabaseB.FindStringExact(cboDatabaseA.Text);
        }

        private void CboDatabaseB_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetObjectsB();
            cboObjectB.SelectedIndex = cboObjectB.FindStringExact(cboObjectA.Text);
        }

        private void CboDatabaseA_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetObjectsA();
        }

        private void CboObjectTypeA_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetObjectsA();
        }

        private void CboObjectTypeB_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetObjectsB();
        }

        private void CboObjectA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboObjectA.SelectedValue != null)
            {
                long ObjectID = (long)cboObjectA.SelectedValue;
                GetSnapshots(cboDate_A, ObjectID);
            }
        }

        private static void GetSnapshots(ComboBox cbo, long ObjectID)
        {
            DataTable dt = CommonData.GetDDLHistoryForObject(ObjectID, 1, 200);
            cbo.DataSource = new BindingSource(dt, null);
            cbo.DisplayMember = "SnapshotValidFrom";
            cbo.ValueMember = "SnapshotValidFrom";
        }

        private void CboObjectB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboObjectB.SelectedValue != null)
            {
                long ObjectID = (long)cboObjectB.SelectedValue;
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
            foreach (Control control in this.Controls)
            {
                control.ApplyTheme(theme);
            }
            panel1.BackColor = theme.PanelBackColor;
            panel1.ForeColor = theme.PanelForeColor;
            diffControl.ApplyTheme(theme);
        }
    }
}