using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace DBAChecksGUI
{
    static class Common
    {
        public static string ConnectionString;

        public static Dictionary<Int32, string> DateGroups = new Dictionary<Int32, string>() {
                {0,"None" },
                { 1, "1min" },
                { 2, "2min" },
                { 5, "5min" },
                { 10, "10min" },
                { 15, "15min" },
                { 30, "30min" },
                { 60, "1hr" },
                { 120, "2hrs" },
                { 240, "4hrs" },
                { 360, "6hrs" },
                { 720, "12hrs" },
                { 1440, "1 Day" }
            };

        public static Guid HighPerformancePowerPlanGUID
        {
            get
            {
                return Guid.Parse("8C5E7FDA-E8BF-4A96-9A85-A6E23A8C635C");
            }
        }

        public static string DateGroupString(Int32 mins)
        {
            return (DateGroups.Where(k => k.Key == mins).First()).Value;
        }

        public static void AddDateGroups(ToolStripDropDownButton tsRoot, EventHandler click)
        {
            foreach (var dg in Common.DateGroups)
            {
                var ts = new ToolStripMenuItem(dg.Value);
                ts.Tag = dg.Key;
                ts.Click += click;
                tsRoot.DropDownItems.Add(ts);
            }
        }

        public static Int32 DateGrouping(Int32 Mins, Int32 MaxPoints)
        {
            Int32 lastMins = 0;
         
            foreach (var mins in Common.DateGroups.OrderBy(k => k.Key)
                .Select(k => k.Key)
                .ToList())
            {
                double div = mins == 0 ? 0.2 : mins;
                if (Mins / div < MaxPoints)
                {
                    return mins;
                }
                lastMins = mins;
            }
            return lastMins;
        }

        public static string DDL(Int64 DDLID,string connectionString)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                string sql = @"SELECT DDL
FROM dbo.DDL
WHERE DDLID = @DDLID";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("DDLID", DDLID);
                var bDDL = (byte[])cmd.ExecuteScalar();

                return DBAChecks.SchemaSnapshotDB.Unzip(bDDL);

            }
        }

        public static Int32 UtcOffset
        {
            get
            {
                return (Int32)DateTime.Now.Subtract(DateTime.UtcNow).TotalMinutes;
            }
        }

        public static Int32 GetDatabaseID(string instance, string dbName)
        {
            if (instance == null || instance.Length == 0 || dbName == null || dbName.Length == 0)
            {
                return -1;
            }
            else
            {
                SqlConnection cn = new SqlConnection(Common.ConnectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("DatabaseID_Get", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("Instance", instance);
                    cmd.Parameters.AddWithValue("DBName", dbName);
                    return (Int32)cmd.ExecuteScalar();
                }
            }
        }

        public static DataTable ConvertUTCToLocal(ref DataTable dt,List<string>convertCols=null)
        {
            List<Int32> convertColsIdx = new List<int>();
            if (convertCols == null || convertCols.Count == 0) {
                convertCols = new List<string>();
                foreach (DataColumn col in dt.Columns)
                {
                    if ( col.DataType == typeof(DateTime))
                    {
                        convertColsIdx.Add(col.Ordinal);
                    }
                }
            }
            else
            {
                foreach(string col in convertCols)
                {
                    convertColsIdx.Add(dt.Columns[col].Ordinal);
                }
            }
            if (convertColsIdx.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    foreach(var col in convertColsIdx)
                    {
                        if (row[col] != DBNull.Value)
                        {
                            row[col] = ((DateTime)row[col]).ToLocalTime();
                        }
                    }
                }
            }
            return dt;

        }

        public static void CopyHtmlToClipBoard(string html)
        {
            Encoding enc = Encoding.UTF8;

            string begin = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}"
              + "\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";

            string html_begin = "<html>\r\n<head>\r\n"
              + "<meta http-equiv=\"Content-Type\""
              + " content=\"text/html; charset=" + enc.WebName + "\">\r\n"
              + "<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n"
              + "<!--StartFragment-->";

            string html_end = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";

            string begin_sample = String.Format(begin, 0, 0, 0, 0);

            int count_begin = enc.GetByteCount(begin_sample);
            int count_html_begin = enc.GetByteCount(html_begin);
            int count_html = enc.GetByteCount(html);
            int count_html_end = enc.GetByteCount(html_end);

            string html_total = String.Format(
              begin
              , count_begin
              , count_begin + count_html_begin + count_html + count_html_end
              , count_begin + count_html_begin
              , count_begin + count_html_begin + count_html
              ) + html_begin + html + html_end;

            DataObject obj = new DataObject();
            obj.SetData(DataFormats.Html, new MemoryStream(
              enc.GetBytes(html_total)));

            Clipboard.SetDataObject(obj, true);
        }


        public static void CopyDataGridViewToClipboard(DataGridView dgv,string headerBGcolor= "#696969",string headerColor= "#FFFFFF")
        {

            var DataGridView1Counts = dgv.Rows.Count;


            StringBuilder html = new StringBuilder();
            html.Append("<table>");

            if (DataGridView1Counts > 0)
            {
                html.Append("<tr>");
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible)
                    {
                        html.Append(string.Format("<th style=\"background-color:{1};color:{2};\">{0}</th>", col.HeaderText,headerBGcolor,headerColor));
                    }
                }
                html.Append("</tr>");


                foreach (DataGridViewRow row in dgv.Rows)
                {
                    html.Append("<tr>");
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Visible)
                        {
                            html.AppendFormat("<td style=\"background-color:{1}; color:{2};\">{0}</td>", cell.FormattedValue, ColorTranslator.ToHtml(cell.Style.BackColor),ColorTranslator.ToHtml(cell.Style.ForeColor));
                        }
                    }
                    html.Append("</tr>");
                }
            }
            html.Append("</table>");
            CopyHtmlToClipBoard(html.ToString());
        }

        public static DialogResult ShowInputDialog(ref string input, string title)
        {
            System.Drawing.Size size = new System.Drawing.Size(400, 70);
            Form inputBox = new Form();

            inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            inputBox.ClientSize = size;
            inputBox.Text = title;
            inputBox.MaximizeBox = false;
            inputBox.MinimizeBox = false;

            System.Windows.Forms.TextBox textBox = new TextBox();
            textBox.Size = new System.Drawing.Size(size.Width - 10, 23);
            textBox.Location = new System.Drawing.Point(5, 5);
            textBox.Text = input;
            inputBox.Controls.Add(textBox);

            Button okButton = new Button();
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.Text = "&OK";
            okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, 39);
            inputBox.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.Text = "&Cancel";
            cancelButton.Location = new System.Drawing.Point(size.Width - 80, 39);
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            DialogResult result = inputBox.ShowDialog();
            input = textBox.Text;
            return result;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            hex.Append("0x");
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}
