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

namespace DBADashGUI
{

    public partial class DBDiff : Form
    {
  

        public List<int> SelectedTags;

        private string selectedInstance_A;
        private DatabaseItem selectedDB_A;
        public DBDiff()
        {
            InitializeComponent();
        }

        public string SelectedInstanceA
        {
            get
            {
                return cboInstanceA.Text;
            }
            set
            {
                selectedInstance_A = value;
                cboInstanceA.Text = value;
            }
        }
        public string SelectedInstanceB
        {
            get
            {
                return cboInstanceB.Text;
            }
            set
            {
                cboInstanceB.Text = value;
            }
        }

        public DatabaseItem SelectedDatabaseA
        {
            get
            {
                return (DatabaseItem)cboDatabaseA.SelectedItem;
            }
            set
            {
                selectedDB_A = value;
                cboDatabaseA.SelectedItem = value;
            }
        }

        private void getInstances()
        {
            var instances = CommonData.GetInstancesWithDDLSnapshot(SelectedTags);
            cboInstanceA.DataSource = new BindingSource(instances,null);
            cboInstanceB.DataSource = new BindingSource(instances,null);
            cboInstanceA.SelectedItem = selectedInstance_A;

        }



        private void getDatabases(ComboBox cbo, string instance)
        {
            var databases = CommonData.GetDatabasesWithDDLSnapshot(instance);
            cbo.DataSource = databases;
           
        }

        private void cboInstanceB_SelectedIndexChanged(object sender, EventArgs e)
        {
            getDatabases(cboDatabaseB, cboInstanceB.Text);
        }

        private void cboInstanceA_SelectedIndexChanged(object sender, EventArgs e)
        {
   
            getDatabases(cboDatabaseA, cboInstanceA.Text);
            
            cboDatabaseA.SelectedItem = selectedDB_A;
        }

        readonly DiffControl diffControl = new DiffControl();

        private void DBDiff_Load(object sender, EventArgs e)
        {
            chkDiffType.Items.Add("Equal", false);
            chkDiffType.Items.Add("Diff", true);
            chkDiffType.Items.Add("A Only", true);
            chkDiffType.Items.Add("B Only", true);
            splitContainer1.Panel2.Controls.Add(diffControl);
            diffControl.Dock = DockStyle.Fill;
            getInstances();
            getObjectTypes();
        }

        private void getObjectTypes()
        {
            var objtypes = CommonData.GetObjectTypes();
            chkObjectType.Items.Add("{all}", true);
            foreach (string t in objtypes.Values)
            {
                chkObjectType.Items.Add(t, true);
            }       
        }

        private static DataTable DBCompare(int DBID_A, int DBID_B,DateTime Date_A,DateTime Date_B)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabaseDDLCompare_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("DBID_A", DBID_A);
                cmd.Parameters.AddWithValue("DBID_B", DBID_B);
                if (Date_A > DateTime.MinValue)
                {
                    var p = cmd.Parameters.AddWithValue("Date_A", Date_A);
                    p.SqlDbType = SqlDbType.DateTime2;
                }
                else
                {
                    cmd.Parameters.AddWithValue("Date_A", DBNull.Value);
                }

                if (Date_B > DateTime.MinValue)
                {
                    var p = cmd.Parameters.AddWithValue("Date_B", Date_B);
                    p.SqlDbType = SqlDbType.DateTime2;
                }
                else
                {
                    cmd.Parameters.AddWithValue("Date_B", DBNull.Value);
                }

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private int DBID_A
        {
            get
            {
                return ((DatabaseItem)cboDatabaseA.SelectedItem).DatabaseID;
            }
        }

        private int DBID_B
        {
            get
            {
                return ((DatabaseItem)cboDatabaseB.SelectedItem).DatabaseID;
            }
        }

        private DateTime Date_A
        {
            get
            {
                if (cboDate_A.SelectedIndex > 0)
                {
                    return (DateTime)cboDate_A.SelectedItem;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
        }
        private DateTime Date_B
        {
            get
            {
                if (cboDate_B.SelectedIndex > 0)
                {
                    return (DateTime)cboDate_B.SelectedItem;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
        }

        private void bttnCompare_Click(object sender, EventArgs e)
        {
            chkIgnoreWhiteSpace.Checked = false;
            if (cboDatabaseA.SelectedItem != null && cboDatabaseB.SelectedItem != null)
            {
                DataTable dt = DBCompare(DBID_A, DBID_B, Date_A, Date_B);
                dt.Columns.Add("A_Text");
                dt.Columns.Add("B_Text");
                dt.Columns.Add("WhitespaceDiff", typeof(bool));
                gvDiff.AutoGenerateColumns = false;
                string rowFilter = getRowFilter();
                dvDiff = new DataView(dt, rowFilter ,"", DataViewRowState.CurrentRows);
                gvDiff.DataSource = dvDiff;
                gvDiff.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            else
            {
                MessageBox.Show("Select databases to compare", "Select Databases", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        DataView dvDiff;

        private void gvDiff_SelectionChanged(object sender, EventArgs e)
        {
            if (gvDiff.SelectedRows.Count == 1)
            {
                var row = (DataRowView)gvDiff.SelectedRows[0].DataBoundItem;
                getTextForRow(row.Row, out string a, out string b);               
                diffControl.OldText = a;
                diffControl.NewText = b;
            }
        }

        private void getTextForRow(DataRow row, out string a,out string b)
        {
            a = String.Empty;
            b = String.Empty;
            if (row["A_Text"] == DBNull.Value)
            {
                if (row["DDLID_A"] != DBNull.Value)
                {
                    a = Common.DDL((Int64)row["DDLID_A"]);
                }
                if (row["DDLID_B"] != DBNull.Value)
                {
                    b = Common.DDL((Int64)row["DDLID_B"]);
                }
                if ((string)row["DiffType"] == "Diff" && a.Trim() == b.Trim())
                {
                    row["WhitespaceDiff"] = true;
                }
                else
                {
                    row["WhitespaceDiff"] = false;
                }
                row["A_Text"] = a;
                row["B_Text"] = b;
            }
            else
            {
                a = (string)row["A_Text"];
                b = (string)row["B_Text"];
            }
        }



        private string getRowFilter()
        {
            StringBuilder sb = new StringBuilder();
            if (chkObjectType.CheckedItems.Count > 0)
            {
                sb.Append("TypeDescription IN(");
                foreach (string itm in chkObjectType.CheckedItems)
                {
                    if (itm != "{all}") {
                        sb.Append("'" + itm + "',");
                    }
                }

                sb.Remove(sb.Length - 1, 1);
                sb.Append(") ");
            }
            else
            {
                sb.Append("1=2 ");
            }
            sb.Append(" AND ");
            if (chkDiffType.CheckedItems.Count > 0)
            {
                sb.Append("DiffType IN(");
                foreach (string itm in chkDiffType.CheckedItems)
                {
                    sb.Append("'" + itm + "',");
                    if (itm == "Equal")
                    {
                        sb.Append("'Equal (Whitespace)',");
                    }
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append(") ");
            }
            else
            {
                sb.Append("1=2 ");
            }
            return sb.ToString();

        }

        private bool CheckAllState = true;

        private void toggleCheck(bool state)
        {
            for (int i = 0; i < chkObjectType.Items.Count; i++)
                chkObjectType.SetItemCheckState(i, (state ? CheckState.Checked : CheckState.Unchecked));
            CheckAllState = state;
        }

        private void chkObjectType_SelectedValueChanged(object sender, EventArgs e)
        {
            if (chkObjectType.CheckedItems.Contains("{all}") && CheckAllState == false)
            {
                toggleCheck(true);
            }
            if (!chkObjectType.CheckedItems.Contains("{all}") && CheckAllState)
            {
                toggleCheck(false);
            }
            if (gvDiff.DataSource != null)
            {
                dvDiff.RowFilter = getRowFilter();
            }
        }

        private void cboDatabaseA_SelectedIndexChanged(object sender, EventArgs e)
        {
            getSnapshotDates(cboDate_A, ((DatabaseItem)cboDatabaseA.SelectedItem).DatabaseID);
        }

        private void getSnapshotDates(ComboBox cbo, Int32 DatabaseID)
        {
            cbo.Items.Clear();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DDLSnapshotDates_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        cbo.Items.Add((DateTime)rdr[0]);
                    }
                }
            }
        }

        private void cboDatabaseB_SelectedIndexChanged(object sender, EventArgs e)
        {
            getSnapshotDates(cboDate_B, ((DatabaseItem)cboDatabaseB.SelectedItem).DatabaseID);
        }


        private void chkDiffType_SelectedValueChanged(object sender, EventArgs e)
        {
            if (dvDiff != null)
            {
                dvDiff.RowFilter = getRowFilter();
            }
        }

        private void bttnSwitch_Click(object sender, EventArgs e)
        {
            var instanceA = cboInstanceA.SelectedItem;
            var instanceB = cboInstanceB.SelectedItem;
            var db_A = cboDatabaseA.SelectedItem;
            var db_B = cboDatabaseB.SelectedItem;
            var verA = cboDate_A.SelectedItem;
            var verB = cboDate_B.SelectedItem;
            cboInstanceA.SelectedItem = instanceB;
            cboInstanceB.SelectedItem = instanceA;
            cboDatabaseA.SelectedItem = db_B;
            cboDatabaseB.SelectedItem = db_A;
            cboDate_A.SelectedItem = verB;
            cboDate_B.SelectedItem = verA;

        }

        private void bttnCopyA_Click(object sender, EventArgs e)
        {
            cboInstanceB.SelectedItem = cboInstanceA.SelectedItem;
            cboDatabaseB.SelectedItem = cboDatabaseA.SelectedItem;
            cboDate_B.SelectedItem = cboDate_A.SelectedItem;
        }

        private void bttnCopyB_Click(object sender, EventArgs e)
        {
            cboInstanceA.SelectedItem = cboInstanceB.SelectedItem;
            cboDatabaseA.SelectedItem = cboDatabaseB.SelectedItem;
            cboDate_A.SelectedItem = cboDate_B.SelectedItem;
        }

        private void chkIgnoreWhiteSpace_CheckedChanged(object sender, EventArgs e)
        {
            ignoreWhitespace();
        }

        private void ignoreWhitespace()
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                foreach (DataRow r in dvDiff.Table.Rows)
                {
                    if ((string)r["DiffType"] == "Diff" && r["WhitespaceDiff"] == DBNull.Value)
                    {
                        getTextForRow(r, out _, out _);
                    }
                    if (r["WhitespaceDiff"] != DBNull.Value && (bool)r["WhitespaceDiff"] == true)
                    {
                        r["DiffType"] = chkIgnoreWhiteSpace.Checked ? "Equal (Whitespace)" : "Diff";

                    }
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

    }
}
