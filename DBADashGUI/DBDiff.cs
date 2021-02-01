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

namespace DBADashGUI
{

    public partial class DBDiff : Form
    {
        public class DatabaseItem
        {
            public Int32 DatabaseID { get; set; }
            public string DatabaseName { get; set; }

            public override string ToString()
            {
                return DatabaseName;
            }

            public override bool Equals(object obj)
            {
                if (Object.ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj is null)
                {
                    return false;
                }
                if (this.GetType() != obj.GetType())
                {
                    return false;
                }
                return ((DatabaseItem)obj).DatabaseID == this.DatabaseID && ((DatabaseItem)obj).DatabaseName == this.DatabaseName;
            }

            public override int GetHashCode()
            {
                return DatabaseName.GetHashCode();
            }


        }

        public string ConnectionString;
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
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"SELECT Instance
FROM dbo.Instances I
WHERE I.IsActive=1
AND EXISTS(SELECT 1
			FROM dbo.Databases D 
			JOIN dbo.DDLSnapshots SS ON SS.DatabaseID = D.DatabaseID
			WHERE D.InstanceID = I.InstanceID
			)
GROUP BY Instance
ORDER BY Instance
", cn);

                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string instance = (string)rdr[0];
                    cboInstanceA.Items.Add(instance);
                    cboInstanceB.Items.Add(instance);
                    cboInstanceA.SelectedItem = selectedInstance_A;
                }
            }
        }

        private void getDatabases(ComboBox cbo, string instance)
        {
            cbo.Items.Clear();
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"SELECT D.DatabaseID,D.name
FROM dbo.Databases D
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
WHERE I.IsActive=1
AND D.IsActive=1
AND I.Instance = @Instance
AND EXISTS(SELECT 1
			FROM dbo.DDLSnapshots SS 
			WHERE SS.DatabaseID = D.DatabaseID
			)
ORDER BY D.Name
", cn);
                //    cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("Instance", instance);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    cbo.Items.Add(new DatabaseItem() { DatabaseID = (Int32)rdr[0], DatabaseName = (string)rdr[1]});
                }
            }
        }

        private void cboInstanceB_SelectedIndexChanged(object sender, EventArgs e)
        {
            getDatabases(cboDatabaseB, cboInstanceB.Text);
            //cboDatabaseB.SelectedItem = selectedDB_B;

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
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"
SELECT ObjectType,TypeDescription
FROM dbo.ObjectType
ORDER BY TypeDescription
", cn);

                var rdr = cmd.ExecuteReader();
                chkObjectType.Items.Add("{all}", true);
                while (rdr.Read())
                {
                    chkObjectType.Items.Add(rdr[1], true);
                }
            }
        
        }

        private void bttnCompare_Click(object sender, EventArgs e)
        {
            chkIgnoreWhiteSpace.Checked = false;
            if (cboDatabaseA.SelectedItem != null && cboDatabaseB.SelectedItem != null)
            {
                SqlConnection cn = new SqlConnection(ConnectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand(@"WITH A AS (
	SELECT s.ObjectID,
           s.DDLID,
           s.ObjectType,
           s.ObjectName,
           s.SchemaName,
           s.SnapshotDate,
           OT.TypeDescription 
	FROM dbo.DBSchemaAtDate(@DBID_A,@Date_A) s
	JOIN dbo.ObjectType OT ON S.ObjectType = OT.ObjectType
)
,B AS(
	SELECT s.ObjectID,
           s.DDLID,
           s.ObjectType,
           s.ObjectName,
           s.SchemaName,
           s.SnapshotDate,
           OT.TypeDescription 
	FROM dbo.DBSchemaAtDate(@DBID_B,@Date_B) s
	JOIN dbo.ObjectType OT ON S.ObjectType = OT.ObjectType
)
SELECT ISNULL(A.ObjectName,B.ObjectName) AS ObjectName,
		ISNULL(A.SchemaName,B.SchemaName) SchemaName,
		ISNULL(A.ObjectType,B.ObjectType) ObjectType,
        ISNULL(A.TypeDescription,B.TypeDescription) TypeDescription,
		CASE WHEN A.ObjectID IS NULL THEN 'B Only' WHEN B.ObjectID IS NULL THEN 'A Only' WHEN A.DDLID=B.DDLID THEN 'Equal' ELSE 'Diff' END AS DiffType,
		A.DDLID AS DDLID_A,
		B.DDLID AS DDLID_B
FROM A
FULL JOIN B ON A.ObjectName = B.ObjectName AND A.SchemaName = B.SchemaName AND A.ObjectType = B.ObjectType
--WHERE (A.DDLID<> B.DDLID OR A.DDLID IS NULL OR B.DDLID IS NULL)

", cn);
                    //    cmd.CommandType = CommandType.StoredProcedure;


                    cmd.Parameters.AddWithValue("DBID_A", ((DatabaseItem)cboDatabaseA.SelectedItem).DatabaseID);
                    cmd.Parameters.AddWithValue("DBID_B", ((DatabaseItem)cboDatabaseB.SelectedItem).DatabaseID);
                    if (cboDate_A.SelectedIndex > 0)
                    {
                        var p = cmd.Parameters.AddWithValue("Date_A", cboDate_A.SelectedItem);
                        p.SqlDbType = SqlDbType.DateTime2;
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("Date_A", DBNull.Value);
                    }
             
                    if (cboDate_B.SelectedIndex > 0)
                    {
                        var p = cmd.Parameters.AddWithValue("Date_B", cboDate_B.SelectedItem);
                        p.SqlDbType = SqlDbType.DateTime2;
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("Date_B", DBNull.Value);
                    }
                   // cmd.Parameters.AddWithValue("Date_B");
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dt.Columns.Add("A_Text");
                    dt.Columns.Add("B_Text");
                    dt.Columns.Add("WhitespaceDiff", typeof(bool));
                    gvDiff.AutoGenerateColumns = false;
                    string rowFilter = getRowFilter();
                    dvDiff = new DataView(dt, rowFilter ,"", DataViewRowState.CurrentRows);
                    gvDiff.DataSource = dvDiff;
                }
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
                    a = Common.DDL((Int64)row["DDLID_A"], ConnectionString);
                }
                if (row["DDLID_B"] != DBNull.Value)
                {
                    b = Common.DDL((Int64)row["DDLID_B"], ConnectionString);
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
            if (gvDiff.DataSource != null)
            {
                if (chkObjectType.CheckedItems.Contains("{all}") && CheckAllState == false)
                {
                    toggleCheck(true);
                }
                if (!chkObjectType.CheckedItems.Contains("{all}") && CheckAllState)
                {
                    toggleCheck(false);
                }

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
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"
            SELECT TOP(100) SnapshotDate,ValidatedDate
FROM dbo.DDLSnapshots
WHERE DatabaseID = @DatabaseID
ORDER BY ValidatedDate DESC
", cn);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    cbo.Items.Add((DateTime)rdr[0]);
                }
            }
        }

        private void cboDatabaseB_SelectedIndexChanged(object sender, EventArgs e)
        {
            getSnapshotDates(cboDate_B, ((DatabaseItem)cboDatabaseB.SelectedItem).DatabaseID);
        }

        private void cboDate_A_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }

        private void chkDiffType_SelectedIndexChanged(object sender, EventArgs e)
        {

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

        private void chkDiffType_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }
    }
}
