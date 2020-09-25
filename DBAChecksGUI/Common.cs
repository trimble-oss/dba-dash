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
    class Common
    {
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
    }
}
