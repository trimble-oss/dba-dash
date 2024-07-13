using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class DBDiff : Form, IThemedControl
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
            get => cboInstanceA.Text;
            set
            {
                selectedInstance_A = value;
                cboInstanceA.Text = value;
            }
        }

        public string SelectedInstanceB
        {
            get => cboInstanceB.Text; set => cboInstanceB.Text = value;
        }

        public DatabaseItem SelectedDatabaseA
        {
            get => (DatabaseItem)cboDatabaseA.SelectedItem;
            set
            {
                selectedDB_A = value;
                cboDatabaseA.SelectedItem = value;
            }
        }

        private void GetInstances()
        {
            var instances = CommonData.GetInstancesWithDDLSnapshot(SelectedTags);
            cboInstanceA.DataSource = new BindingSource(instances, null);
            cboInstanceB.DataSource = new BindingSource(instances, null);
            cboInstanceA.SelectedItem = selectedInstance_A;
        }

        private static void GetDatabases(ComboBox cbo, string instanceGroupName)
        {
            var databases = CommonData.GetDatabasesWithDDLSnapshot(instanceGroupName);
            cbo.DataSource = databases;
        }

        private void CboInstanceB_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetDatabases(cboDatabaseB, cboInstanceB.Text);
        }

        private void CboInstanceA_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetDatabases(cboDatabaseA, cboInstanceA.Text);

            cboDatabaseA.SelectedItem = selectedDB_A;
        }

        private readonly DiffControl diffControl = new();

        private void DBDiff_Load(object sender, EventArgs e)
        {
            chkDiffType.Items.Add("Equal", false);
            chkDiffType.Items.Add("Diff", true);
            chkDiffType.Items.Add("A Only", true);
            chkDiffType.Items.Add("B Only", true);
            splitContainer1.Panel2.Controls.Add(diffControl);
            diffControl.Dock = DockStyle.Fill;
            GetInstances();
            GetObjectTypes();
        }

        private void GetObjectTypes()
        {
            var objTypes = CommonData.GetObjectTypes();
            chkObjectType.Items.Add("{all}", true);
            foreach (var t in objTypes.Values)
            {
                chkObjectType.Items.Add(t, true);
            }
        }

        private static DataTable DBCompare(int DBID_A, int DBID_B, DateTime Date_A, DateTime Date_B)
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

                SqlDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private int DBID_A => ((DatabaseItem)cboDatabaseA.SelectedItem).DatabaseID;

        private int DBID_B => ((DatabaseItem)cboDatabaseB.SelectedItem).DatabaseID;

        private DateTime Date_A => cboDate_A.SelectedIndex > 0 ? (DateTime)cboDate_A.SelectedItem : DateTime.MinValue;

        private DateTime Date_B => cboDate_B.SelectedIndex > 0 ? (DateTime)cboDate_B.SelectedItem : DateTime.MinValue;

        private void BttnCompare_Click(object sender, EventArgs e)
        {
            chkIgnoreWhiteSpace.Checked = false;
            if (cboDatabaseA.SelectedItem != null && cboDatabaseB.SelectedItem != null)
            {
                var dt = DBCompare(DBID_A, DBID_B, Date_A, Date_B);
                dt.Columns.Add("A_Text");
                dt.Columns.Add("B_Text");
                dt.Columns.Add("WhitespaceDiff", typeof(bool));
                gvDiff.AutoGenerateColumns = false;
                var rowFilter = GetRowFilter();
                dvDiff = new DataView(dt, rowFilter, "", DataViewRowState.CurrentRows);
                gvDiff.DataSource = dvDiff;
                gvDiff.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            else
            {
                MessageBox.Show("Select databases to compare", "Select Databases", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private DataView dvDiff;

        private void GvDiff_SelectionChanged(object sender, EventArgs e)
        {
            if (gvDiff.SelectedRows.Count == 1)
            {
                var row = (DataRowView)gvDiff.SelectedRows[0].DataBoundItem;
                GetTextForRow(row.Row, out var a, out var b);
                diffControl.OldText = a;
                diffControl.NewText = b;
            }
        }

        private static void GetTextForRow(DataRow row, out string a, out string b)
        {
            a = string.Empty;
            b = string.Empty;
            if (row["A_Text"] == DBNull.Value)
            {
                if (row["DDLID_A"] != DBNull.Value)
                {
                    a = Common.DDL((long)row["DDLID_A"]);
                }
                if (row["DDLID_B"] != DBNull.Value)
                {
                    b = Common.DDL((long)row["DDLID_B"]);
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

        private string GetRowFilter()
        {
            StringBuilder sb = new();
            if (chkObjectType.CheckedItems.Count > 0)
            {
                sb.Append("TypeDescription IN(");
                foreach (string itm in chkObjectType.CheckedItems)
                {
                    if (itm != "{all}")
                    {
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

        private void ToggleCheck(bool state)
        {
            for (var i = 0; i < chkObjectType.Items.Count; i++)
                chkObjectType.SetItemCheckState(i, (state ? CheckState.Checked : CheckState.Unchecked));
            CheckAllState = state;
        }

        private void ChkObjectType_SelectedValueChanged(object sender, EventArgs e)
        {
            if (chkObjectType.CheckedItems.Contains("{all}") && CheckAllState == false)
            {
                ToggleCheck(true);
            }
            if (!chkObjectType.CheckedItems.Contains("{all}") && CheckAllState)
            {
                ToggleCheck(false);
            }
            if (gvDiff.DataSource != null)
            {
                dvDiff.RowFilter = GetRowFilter();
            }
        }

        private void CboDatabaseA_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetSnapshotDates(cboDate_A, ((DatabaseItem)cboDatabaseA.SelectedItem).DatabaseID);
        }

        private static void GetSnapshotDates(ComboBox cbo, int DatabaseID)
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

        private void CboDatabaseB_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetSnapshotDates(cboDate_B, ((DatabaseItem)cboDatabaseB.SelectedItem).DatabaseID);
        }

        private void ChkDiffType_SelectedValueChanged(object sender, EventArgs e)
        {
            if (dvDiff != null)
            {
                dvDiff.RowFilter = GetRowFilter();
            }
        }

        private void BttnSwitch_Click(object sender, EventArgs e)
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

        private void BttnCopyA_Click(object sender, EventArgs e)
        {
            cboInstanceB.SelectedItem = cboInstanceA.SelectedItem;
            cboDatabaseB.SelectedItem = cboDatabaseA.SelectedItem;
            cboDate_B.SelectedItem = cboDate_A.SelectedItem;
        }

        private void BttnCopyB_Click(object sender, EventArgs e)
        {
            cboInstanceA.SelectedItem = cboInstanceB.SelectedItem;
            cboDatabaseA.SelectedItem = cboDatabaseB.SelectedItem;
            cboDate_A.SelectedItem = cboDate_B.SelectedItem;
        }

        private void ChkIgnoreWhiteSpace_CheckedChanged(object sender, EventArgs e)
        {
            IgnoreWhitespace();
        }

        private void IgnoreWhitespace()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                foreach (DataRow r in dvDiff.Table.Rows)
                {
                    if ((string)r["DiffType"] == "Diff" && r["WhitespaceDiff"] == DBNull.Value)
                    {
                        GetTextForRow(r, out _, out _);
                    }
                    if (r["WhitespaceDiff"] != DBNull.Value && (bool)r["WhitespaceDiff"])
                    {
                        r["DiffType"] = chkIgnoreWhiteSpace.Checked ? "Equal (Whitespace)" : "Diff";
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            foreach (Control control in Controls)
            {
                control.ApplyTheme(theme);
            }
            panel1.BackColor = theme.PanelBackColor;
            panel1.ForeColor = theme.PanelForeColor;
            diffControl.ApplyTheme(theme);
        }
    }
}