using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class DDLCompareTo : Form
    {

        readonly DiffControl diffControl = new DiffControl();

        public List<int> SelectedTags;

        public int DatabaseID_A;
        public string Instance_A;
        public long ObjectID_A;
        public string ObjectType_A="P";
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

            cboInstanceA.DataSource = new BindingSource(instances,null);
            
            cboInstanceB.DataSource = new BindingSource(instances,null);

            
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

        private void getObjectsB()
        {
            if (cboDatabaseB.SelectedValue != null && cboObjectTypeB.SelectedValue != null)
            {
                getObjects(cboObjectB, (int)cboDatabaseB.SelectedValue, (string)cboObjectTypeB.SelectedValue);
            }
        }

        private void getObjectsA()
        {
            if (cboDatabaseA.SelectedValue != null && cboObjectTypeA.SelectedValue != null)
            {
                getObjects(cboObjectA, (int)cboDatabaseA.SelectedValue, (string)cboObjectTypeA.SelectedValue);
            }       
        }

        int DropDownWidth(ComboBox myCombo)
        {
            int maxWidth = 0, temp = 0;
            foreach (var obj in myCombo.Items)
            {
                temp = TextRenderer.MeasureText(myCombo.GetItemText(obj), myCombo.Font).Width;
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
            else {
                return maxWidth;
            }
        }

        private void getObjects(ComboBox cbo,int DatabaseID,string type)
        {
            DataTable dt =  CommonData.GetDBObjects(DatabaseID, type);
            cbo.DataSource = new BindingSource(dt,null);
            cbo.DisplayMember = "FullName";
            cbo.ValueMember = "ObjectID";
            cbo.DropDownWidth = DropDownWidth(cbo);
        }


        private void getDatabases(ComboBox cbo, string instanceGroupName)
        {
            var databases = CommonData.GetDatabasesWithDDLSnapshot(instanceGroupName);
            cbo.DataSource = databases;
            cbo.ValueMember = "DatabaseID";
            cbo.DisplayMember = "DatabaseName";

        }

        private void cboInstanceA_SelectedIndexedChanged(object sender, EventArgs e)
        {
            getDatabases(cboDatabaseA, cboInstanceA.Text);
        }

        private void cboInstanceB_SelectedIndexedChanged(object sender, EventArgs e)
        {
            getDatabases(cboDatabaseB, cboInstanceB.Text);
            cboDatabaseB.SelectedIndex = cboDatabaseB.FindStringExact(cboDatabaseA.Text);
        }

        private void cboDatabaseB_SelectedIndexChanged(object sender, EventArgs e)
        {
            getObjectsB();
            cboObjectB.SelectedIndex = cboObjectB.FindStringExact(cboObjectA.Text);
        }

        private void cboDatabaseA_SelectedIndexChanged(object sender, EventArgs e)
        {
            getObjectsA();
        }

        private void cboObjectTypeA_SelectedIndexChanged(object sender, EventArgs e)
        {
            getObjectsA();
        }

        private void cboObjectTypeB_SelectedIndexChanged(object sender, EventArgs e)
        {
            getObjectsB();
        }

        private void cboObjectA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboObjectA.SelectedValue != null)
            {
                long ObjectID = (Int64)cboObjectA.SelectedValue;
                getSnapshots(cboDate_A, ObjectID);
            }
        }

        private void getSnapshots(ComboBox cbo,long ObjectID)
        {
            DataTable dt = CommonData.GetDDLHistoryForObject(ObjectID, 1, 200);
            cbo.DataSource = new BindingSource(dt, null);
            cbo.DisplayMember = "SnapshotValidFrom";
            cbo.ValueMember = "SnapshotValidFrom";
        }

        private void cboObjectB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboObjectB.SelectedValue != null)
            {
                long ObjectID = (Int64)cboObjectB.SelectedValue;
                getSnapshots(cboDate_B, ObjectID);
            }
        }

        private void cboDate_A_SelectedIndexChanged(object sender, EventArgs e)
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

        private void cboDate_B_SelectedIndexChanged(object sender, EventArgs e)
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
    }
}
